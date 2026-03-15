using HoundsBite.Services;
using System.Collections.ObjectModel;

namespace HoundsBite.Views;

public partial class DisplayIngredientsPage : ContentPage
{
    private readonly SupabaseService _supa;
    public ObservableCollection<IngredientCheckModel> Ingredients { get; set; } = new();
    private List<IngredientCheckModel> AllIngredients { get; set; } = new();

    private int _currentUserId = 0;

    public class IngredientCheckModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsLoggedIn { get; set; }
    }

    public DisplayIngredientsPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
        IngredientList.ItemsSource = Ingredients;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadIngredients();
    }

    private async Task LoadIngredients()
    {
        Ingredients.Clear();
        AllIngredients.Clear();

        var ingredients = await _supa.GetAllIngredientsAsync();

        _currentUserId = Preferences.Get("LoggedUserId", 0);
        bool isLoggedIn = _currentUserId > 0;

        List<int> userIngredientIds = new();
        if (isLoggedIn)
        {
            userIngredientIds = await _supa.GetUserIngredientIdsAsync(_currentUserId);
        }

        foreach (var ingredient in ingredients)
        {
            var item = new IngredientCheckModel
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                IsSelected = userIngredientIds.Contains(ingredient.Id),
                IsLoggedIn = isLoggedIn
            };
            AllIngredients.Add(item);
            Ingredients.Add(item);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        Ingredients.Clear();

        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? AllIngredients
            : AllIngredients.Where(i => i.Name.ToLower().Contains(searchText));

        foreach (var item in filtered)
        {
            Ingredients.Add(item);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }

    private async void OnIngredientCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_currentUserId == 0) return;

        var checkBox = (CheckBox)sender;
        var item = (IngredientCheckModel)checkBox.BindingContext;

        if (e.Value)
            await _supa.AddUserIngredientAsync(_currentUserId, item.Id);
        else
            await _supa.RemoveUserIngredientAsync(_currentUserId, item.Id);
    }
}
