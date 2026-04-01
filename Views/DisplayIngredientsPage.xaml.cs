using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HoundsBite.Models;
using HoundsBite.Services;

namespace HoundsBite.Views;

public partial class DisplayIngredientsPage : ContentPage
{
    private readonly SupabaseService _supa;
    private readonly FoodApiService _foodApi;

    public ObservableCollection<IngredientListModel> Ingredients { get; } = new();
    private readonly List<IngredientListModel> _allLocal = new();
    private readonly List<IngredientListModel> _lastApiOnlyRows = new();

    private HashSet<int> _userIngredientIds = new();
    private int _currentUserId;
    private bool _isAdmin;
    private bool _refreshingDisplay;
    private bool _addPanelOpen;

    private static string KitchenQtyKey(int userId) => $"KitchenQty_{userId}";

    private static Dictionary<int, (string A, string U)> LoadQtyMap(int userId)
    {
        var json = Preferences.Get(KitchenQtyKey(userId), "");
        if (string.IsNullOrEmpty(json)) return new Dictionary<int, (string, string)>();
        try
        {
            var rows = JsonSerializer.Deserialize<List<QtyRow>>(json);
            if (rows == null) return new Dictionary<int, (string, string)>();
            return rows.ToDictionary(r => r.Id, r => (r.A ?? "", r.U ?? ""));
        }
        catch
        {
            return new Dictionary<int, (string, string)>();
        }
    }

    private static void SaveQtyMap(int userId, Dictionary<int, (string A, string U)> map)
    {
        var rows = map.Select(kv => new QtyRow { Id = kv.Key, A = kv.Value.A, U = kv.Value.U }).ToList();
        Preferences.Set(KitchenQtyKey(userId), JsonSerializer.Serialize(rows));
    }

    private void PersistQuantityFor(IngredientListModel item)
    {
        if (_currentUserId <= 0 || item.LocalId <= 0) return;
        var map = LoadQtyMap(_currentUserId);
        map[item.LocalId] = (item.Quantity ?? "", item.Unit ?? "");
        SaveQtyMap(_currentUserId, map);
    }

    private sealed class QtyRow
    {
        public int Id { get; set; }
        public string? A { get; set; }
        public string? U { get; set; }
    }

    public class IngredientListModel : INotifyPropertyChanged
    {
        private int _localId;
        private bool _fromApiOnly;

        public int LocalId
        {
            get => _localId;
            set
            {
                if (_localId == value) return;
                _localId = value;
                OnPropertyChanged(nameof(ShowDeleteButton));
            }
        }

        public int ExternalNumericId { get; set; }
        public string? ExternalIdStr { get; set; }

        public bool FromApiOnly
        {
            get => _fromApiOnly;
            set
            {
                if (_fromApiOnly == value) return;
                _fromApiOnly = value;
                OnPropertyChanged(nameof(ShowDeleteButton));
            }
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _name = string.Empty;
        private string _imageUrl = string.Empty;
        private bool _isSelected;
        private bool _isLoggedIn;
        private bool _isAdmin;
        private string _quantity = "";
        private string _unit = "";

        public string ImageUrl
        {
            get => _imageUrl;
            set { if (_imageUrl != value) { _imageUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasImage)); } }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KitchenButtonText));
                OnPropertyChanged(nameof(KitchenButtonColor));
                OnPropertyChanged(nameof(ShowDeleteButton));
            }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn == value) return;
                _isLoggedIn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowDeleteButton));
            }
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin == value) return;
                _isAdmin = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowDeleteButton));
            }
        }

        public string Quantity
        {
            get => _quantity;
            set { if (_quantity != value) { _quantity = value ?? ""; OnPropertyChanged(); } }
        }

        public string Unit
        {
            get => _unit;
            set { if (_unit != value) { _unit = value ?? ""; OnPropertyChanged(); } }
        }

        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

        public bool ShowDeleteButton =>
            IsLoggedIn && (FromApiOnly || IsSelected || (IsAdmin && LocalId > 0 && !FromApiOnly));

        public string KitchenButtonText => _isSelected ? "✓ In kitchen" : "+ Kitchen";
        public Color KitchenButtonColor => _isSelected
            ? Color.FromArgb("#2E7D32")
            : Color.FromArgb("#1E3A5F");

        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public DisplayIngredientsPage(SupabaseService supa, FoodApiService foodApi)
    {
        InitializeComponent();
        _supa = supa;
        _foodApi = foodApi;
        IngredientList.ItemsSource = Ingredients;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Preferences.Get("LoggedUserId", 0) == 0)
        {
            await DisplayAlert(
                "Login required",
                "Please log in from Profile to view your kitchen inventory.",
                "OK");
            await Shell.Current.GoToAsync("//home");
            return;
        }

        _currentUserId = Preferences.Get("LoggedUserId", 0);
        _isAdmin = Preferences.Get("UserRole", "") == "Admin";
        _ = LoadLocalCacheAndRefreshAsync();
    }

    private async Task LoadLocalCacheAndRefreshAsync()
    {
        try
        {
            _allLocal.Clear();
            _lastApiOnlyRows.Clear();

            var dbIngredients = await _supa.GetAllIngredientsAsync();
            var isLoggedIn = _currentUserId > 0;
            var qtyMap = isLoggedIn ? LoadQtyMap(_currentUserId) : new Dictionary<int, (string, string)>();

            if (isLoggedIn)
                _userIngredientIds = (await _supa.GetUserIngredientIdsAsync(_currentUserId)).ToHashSet();
            else
                _userIngredientIds = new HashSet<int>();

            foreach (var ing in dbIngredients)
            {
                int.TryParse(ing.ExternalId, out var extNum);
                if (!qtyMap.TryGetValue(ing.Id, out var qu))
                    qu = ("", "");
                _allLocal.Add(new IngredientListModel
                {
                    LocalId = ing.Id,
                    ExternalNumericId = extNum,
                    ExternalIdStr = ing.ExternalId,
                    Name = ing.Name,
                    ImageUrl = ing.ImageUrl ?? "",
                    IsSelected = isLoggedIn && _userIngredientIds.Contains(ing.Id),
                    IsLoggedIn = isLoggedIn,
                    IsAdmin = _isAdmin,
                    FromApiOnly = false,
                    Quantity = qu.Item1,
                    Unit = qu.Item2
                });
            }

            RefreshDisplayedList();
        }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert("Error", "Could not load ingredients.", "OK"));
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _lastApiOnlyRows.Clear();
        RefreshDisplayedList();
    }

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        var query = SearchBar.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            await DisplayAlert("Search", "Enter a name to search the food database.", "OK");
            return;
        }

        _lastApiOnlyRows.Clear();
        ApiLoadingIndicator.IsVisible = true;
        ApiLoadingIndicator.IsRunning = true;

        try
        {
            var apiResults = await _foodApi.SearchIngredientsAsync(query);
            var isLoggedIn = _currentUserId > 0;

            foreach (var res in apiResults)
            {
                if (_allLocal.Any(l =>
                        (l.ExternalIdStr == res.Id.ToString()) ||
                        l.Name.Equals(res.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (_lastApiOnlyRows.Any(r => r.ExternalNumericId == res.Id))
                    continue;

                _lastApiOnlyRows.Add(new IngredientListModel
                {
                    LocalId = 0,
                    ExternalNumericId = res.Id,
                    ExternalIdStr = res.Id.ToString(),
                    Name = res.Name,
                    ImageUrl = res.ImageUrl,
                    FromApiOnly = true,
                    IsLoggedIn = isLoggedIn,
                    IsAdmin = _isAdmin,
                    IsSelected = false,
                    Quantity = "",
                    Unit = ""
                });
            }

            RefreshDisplayedList();

            if (apiResults.Count == 0)
                await DisplayAlert("No results", "No matching ingredients in the global database for that search.", "OK");
        }
        finally
        {
            ApiLoadingIndicator.IsVisible = false;
            ApiLoadingIndicator.IsRunning = false;
        }
    }

    private void RefreshDisplayedList()
    {
        if (_refreshingDisplay) return;
        _refreshingDisplay = true;

        try
        {
            var q = SearchBar.Text?.Trim() ?? "";
            var useFilter = !string.IsNullOrWhiteSpace(q);
            var qLower = q.ToLowerInvariant();

            IEnumerable<IngredientListModel> locals = _allLocal;
            if (useFilter)
                locals = _allLocal.Where(l => l.Name.ToLowerInvariant().Contains(qLower));

            Ingredients.Clear();

            foreach (var item in locals)
            {
                item.IsLoggedIn = _currentUserId > 0;
                item.IsAdmin = _isAdmin;
                item.IsSelected = _currentUserId > 0 && _userIngredientIds.Contains(item.LocalId);
                Ingredients.Add(item);
            }

            if (useFilter)
            {
                foreach (var row in _lastApiOnlyRows)
                {
                    row.IsLoggedIn = _currentUserId > 0;
                    row.IsAdmin = _isAdmin;
                    Ingredients.Add(row);
                }
            }
        }
        finally
        {
            _refreshingDisplay = false;
        }
    }

    private void OnQuantityUnfocused(object? sender, FocusEventArgs e)
    {
        if (sender is VisualElement ve && ve.BindingContext is IngredientListModel item)
            PersistQuantityFor(item);
    }

    private async void OnKitchenToggleClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not IngredientListModel item)
            return;

        if (_currentUserId == 0)
        {
            await DisplayAlert("Login required", "Log in to track what you have in your kitchen.", "OK");
            return;
        }

        if (item.LocalId > 0)
        {
            if (!item.IsSelected)
            {
                await _supa.AddUserIngredientAsync(_currentUserId, item.LocalId);
                _userIngredientIds.Add(item.LocalId);
                item.IsSelected = true;
                PersistQuantityFor(item);
            }
            else
            {
                await _supa.RemoveUserIngredientAsync(_currentUserId, item.LocalId);
                _userIngredientIds.Remove(item.LocalId);
                item.IsSelected = false;
            }

            return;
        }

        if (!item.FromApiOnly || item.ExternalNumericId <= 0)
            return;

        btn.IsEnabled = false;
        try
        {
            var localId = await EnsureApiIngredientInDbAndKitchenAsync(item);
            if (localId > 0)
            {
                item.LocalId = localId;
                item.FromApiOnly = false;
                item.IsAdmin = _isAdmin;
                item.IsSelected = true;
                _userIngredientIds.Add(localId);
                _allLocal.Add(item);
                _lastApiOnlyRows.RemoveAll(r => r.ExternalNumericId == item.ExternalNumericId);
                PersistQuantityFor(item);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not IngredientListModel item)
            return;

        if (_currentUserId == 0)
        {
            await DisplayAlert("Login required", "Log in to manage your kitchen.", "OK");
            return;
        }

        if (item.FromApiOnly)
        {
            _lastApiOnlyRows.RemoveAll(r => r.ExternalNumericId == item.ExternalNumericId);
            Ingredients.Remove(item);
            return;
        }

        if (item.LocalId <= 0)
            return;

        if (_isAdmin)
        {
            if (item.IsSelected)
            {
                var action = await DisplayActionSheet(
                    $"\"{item.Name}\"",
                    "Cancel",
                    "Delete from catalog",
                    "Remove from my kitchen");

                if (action == "Remove from my kitchen")
                {
                    await _supa.RemoveUserIngredientAsync(_currentUserId, item.LocalId);
                    _userIngredientIds.Remove(item.LocalId);
                    item.IsSelected = false;
                    var mapRm = LoadQtyMap(_currentUserId);
                    mapRm.Remove(item.LocalId);
                    SaveQtyMap(_currentUserId, mapRm);
                    return;
                }

                if (action != "Delete from catalog")
                    return;
            }

            var confirmDel = await DisplayAlert(
                "Delete from catalog",
                $"Permanently delete \"{item.Name}\" for all users and recipes?",
                "Delete", "Cancel");
            if (!confirmDel) return;

            btn.IsEnabled = false;
            try
            {
                await _supa.DeleteIngredientAsync(item.LocalId);
                var map = LoadQtyMap(_currentUserId);
                map.Remove(item.LocalId);
                SaveQtyMap(_currentUserId, map);

                _allLocal.RemoveAll(i => i.LocalId == item.LocalId);
                _userIngredientIds.Remove(item.LocalId);
                Ingredients.Remove(item);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                btn.IsEnabled = true;
            }

            return;
        }

        if (!item.IsSelected)
            return;

        var ok = await DisplayAlert(
            "Remove from kitchen",
            $"Remove \"{item.Name}\" from your kitchen?",
            "Remove", "Cancel");
        if (!ok) return;

        await _supa.RemoveUserIngredientAsync(_currentUserId, item.LocalId);
        _userIngredientIds.Remove(item.LocalId);
        item.IsSelected = false;

        var mapUser = LoadQtyMap(_currentUserId);
        mapUser.Remove(item.LocalId);
        SaveQtyMap(_currentUserId, mapUser);
    }

    private void OnAddPanelToggle(object sender, EventArgs e)
    {
        _addPanelOpen = !_addPanelOpen;
        AddIngredientForm.IsVisible = _addPanelOpen;
        AddPanelToggle.Text = _addPanelOpen ? "✕ Cancel" : "＋ Add product";
    }

    private async void OnAddIngredientClicked(object sender, EventArgs e)
    {
        var name = NewIngredientEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Validation", "Please enter a name.", "OK");
            return;
        }

        if (_currentUserId == 0)
        {
            await DisplayAlert("Login required", "Log in to add products to your kitchen.", "OK");
            return;
        }

        if (_allLocal.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Duplicate", $"\"{name}\" is already in the catalog.", "OK");
            return;
        }

        var qty = NewQtyEntry.Text?.Trim() ?? "";
        var unit = NewUnitEntry.Text?.Trim() ?? "";

        try
        {
            var added = await _supa.AddIngredientAsync(new Ingredient { Name = name });

            var map = LoadQtyMap(_currentUserId);
            map[added.Id] = (qty, unit);
            SaveQtyMap(_currentUserId, map);

            await _supa.AddUserIngredientAsync(_currentUserId, added.Id);
            _userIngredientIds.Add(added.Id);

            var model = new IngredientListModel
            {
                LocalId = added.Id,
                Name = added.Name,
                ImageUrl = added.ImageUrl ?? "",
                IsLoggedIn = true,
                IsAdmin = _isAdmin,
                IsSelected = true,
                FromApiOnly = false,
                Quantity = qty,
                Unit = unit
            };

            _allLocal.Add(model);

            NewIngredientEntry.Text = "";
            NewQtyEntry.Text = "";
            NewUnitEntry.Text = "";
            _addPanelOpen = false;
            AddIngredientForm.IsVisible = false;
            AddPanelToggle.Text = "＋ Add product";

            RefreshDisplayedList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not add: {ex.Message}", "OK");
        }
    }

    private async Task<int> EnsureApiIngredientInDbAndKitchenAsync(IngredientListModel item)
    {
        var externalIdStr = item.ExternalNumericId.ToString();
        var cached = await _supa.GetIngredientByExternalIdAsync(externalIdStr);

        Ingredient resolved;
        if (cached != null)
        {
            resolved = cached;
        }
        else
        {
            var details = await _foodApi.GetIngredientInformationAsync(item.ExternalNumericId);
            var newIng = new Ingredient
            {
                Name = item.Name,
                ExternalId = externalIdStr,
                ImageUrl = details?.ImageUrl ?? item.ImageUrl,
                Category = details?.Aisle,
                Calories = details?.Calories,
                Protein = details?.Protein,
                Carbs = details?.Carbs,
                Fat = details?.Fat
            };
            resolved = await _supa.AddIngredientAsync(newIng);
        }

        if (!string.IsNullOrWhiteSpace(resolved.ImageUrl))
            item.ImageUrl = resolved.ImageUrl!;

        await _supa.AddUserIngredientAsync(_currentUserId, resolved.Id);
        return resolved.Id;
    }
}
