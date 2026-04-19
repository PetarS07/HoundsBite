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

    // Session-only kitchen for guests
    private static List<IngredientListModel> _tempGuestKitchen = new();

    private enum TabMode { MyKitchen, Search }
    private TabMode _currentTab = TabMode.MyKitchen;

    private HashSet<int> _userIngredientIds = new();
    private int _currentUserId;
    private bool _isAdmin;
    private bool _refreshingDisplay;
    private bool _addPanelOpen;


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

        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

        public bool ShowDeleteButton =>
            IsLoggedIn && (FromApiOnly || IsSelected || (IsAdmin && LocalId > 0 && !FromApiOnly));

        public string KitchenButtonText => _isSelected ? "✓Added to Kitchen" : "Add to Kitchen";
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

            if (isLoggedIn)
            {
                _userIngredientIds = (await _supa.GetUserIngredientIdsAsync(_currentUserId)).ToHashSet();

                foreach (var ing in dbIngredients)
                {
                    int.TryParse(ing.ExternalId, out var extNum);
                    _allLocal.Add(new IngredientListModel
                    {
                        LocalId = ing.Id,
                        ExternalNumericId = extNum,
                        ExternalIdStr = ing.ExternalId,
                        Name = ing.Name,
                        ImageUrl = ing.ImageUrl ?? "",
                        IsSelected = _userIngredientIds.Contains(ing.Id),
                        IsLoggedIn = true,
                        IsAdmin = _isAdmin,
                        FromApiOnly = false
                    });
                }
            }
            else
            {
                // Load guest temp items
                _allLocal.AddRange(_tempGuestKitchen);
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

    private void OnTabMyKitchenClicked(object sender, EventArgs e)
    {
        if (_currentTab == TabMode.MyKitchen) return;
        _currentTab = TabMode.MyKitchen;
        UpdateTabStyles();
        
        SearchPanel.IsVisible = false;
        AddPanelContainer.IsVisible = false;
        HelpText.Text = "Manage the ingredients you currently have in your kitchen.";

        RefreshDisplayedList();
    }

    private void OnTabSearchClicked(object sender, EventArgs e)
    {
        if (_currentTab == TabMode.Search) return;
        _currentTab = TabMode.Search;
        UpdateTabStyles();
        
        SearchPanel.IsVisible = true;
        AddPanelContainer.IsVisible = true;
        HelpText.Text = "Search to find ingredients in the food database and add them to your kitchen.";

        RefreshDisplayedList();
    }

    private void UpdateTabStyles()
    {
        Color pColor = Color.FromArgb("#512BD4");
        if (Application.Current != null && Application.Current.Resources.TryGetValue("Primary", out var res) && res is Color themeColor)
        {
            pColor = themeColor;
        }

        if (_currentTab == TabMode.MyKitchen)
        {
            TabMyKitchen.BackgroundColor = pColor;
            TabMyKitchen.TextColor = Colors.White;
            TabSearch.BackgroundColor = Color.FromArgb("#1E1E1E");
            TabSearch.TextColor = pColor;
        }
        else
        {
            TabSearch.BackgroundColor = pColor;
            TabSearch.TextColor = Colors.White;
            TabMyKitchen.BackgroundColor = Color.FromArgb("#1E1E1E");
            TabMyKitchen.TextColor = pColor;
        }
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
                    IsSelected = false
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
            Ingredients.Clear();

            if (_currentTab == TabMode.MyKitchen)
            {
                foreach (var item in _allLocal.Where(i => _userIngredientIds.Contains(i.LocalId)))
                {
                    item.IsLoggedIn = _currentUserId > 0;
                    item.IsAdmin = _isAdmin;
                    item.IsSelected = true;
                    Ingredients.Add(item);
                }
            }
            else
            {
                var q = SearchBar.Text?.Trim() ?? "";
                var useFilter = !string.IsNullOrWhiteSpace(q);
                var qLower = q.ToLowerInvariant();

                IEnumerable<IngredientListModel> locals = _allLocal;
                if (useFilter)
                    locals = _allLocal.Where(l => l.Name.ToLowerInvariant().Contains(qLower));

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
        }
        finally
        {
            _refreshingDisplay = false;
        }
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
                if (_currentUserId > 0)
                    await _supa.AddUserIngredientAsync(_currentUserId, item.LocalId);
                else
                    _tempGuestKitchen.Add(item);

                item.IsSelected = true;
            }
            else
            {
                if (_currentUserId > 0)
                    await _supa.RemoveUserIngredientAsync(_currentUserId, item.LocalId);
                else
                    _tempGuestKitchen.Remove(item);

                item.IsSelected = false;
                
                if (_currentTab == TabMode.MyKitchen)
                    Ingredients.Remove(item);
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

        if (item.FromApiOnly && _currentUserId == 0)
        {
            // Allowed for guests too
        }
        else if (_currentUserId == 0 && !item.FromApiOnly)
        {
            // Allow guest to remove from their temp kitchen
             _tempGuestKitchen.Remove(item);
             Ingredients.Remove(item);
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
        
        if (_currentTab == TabMode.MyKitchen)
            Ingredients.Remove(item);
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
             var guestModel = new IngredientListModel
             {
                 LocalId = _tempGuestKitchen.Count + 20000,
                 Name = name,
                 ImageUrl = "",
                 IsLoggedIn = true,
                 IsAdmin = false,
                 IsSelected = true,
                 FromApiOnly = false
             };
             _tempGuestKitchen.Add(guestModel);
             _allLocal.Add(guestModel);
             
             NewIngredientEntry.Text = "";
             _addPanelOpen = false;
             AddIngredientForm.IsVisible = false;
             AddPanelToggle.Text = "＋ Add product";
             RefreshDisplayedList();
             return;
        }

        if (_allLocal.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Duplicate", $"\"{name}\" is already in the catalog.", "OK");
            return;
        }

        try
        {
            var added = await _supa.AddIngredientAsync(new Ingredient { Name = name });

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
                FromApiOnly = false
            };

            _allLocal.Add(model);

            NewIngredientEntry.Text = "";
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

        if (_currentUserId > 0)
        {
            await _supa.AddUserIngredientAsync(_currentUserId, resolved.Id);
        }
        else
        {
             // Guest doesn't save to DB kitchen links, item is already added to _tempGuestKitchen in caller
        }
        return resolved.Id;
    }
}
