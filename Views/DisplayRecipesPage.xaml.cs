using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class DisplayRecipesPage : ContentPage
{
    private readonly SupabaseService _supa;
    private bool _filterActive = false;
    private bool _favoritesOnly = false;
    private int _currentUserId = 0;
    private string _searchText = "";
    private List<int> _userFavoriteRecipeIds = new();

    public DisplayRecipesPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _currentUserId = Preferences.Get("LoggedUserId", 0);
        FilterToggleContainer.IsVisible = _currentUserId > 0;
        FavoritesToggleContainer.IsVisible = _currentUserId > 0; // Only logged in users can favorite

        if (CategoryFilter.SelectedIndex == -1)
            CategoryFilter.SelectedIndex = 0;

        await LoadRecipes();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//home");

    private async void OnFilterToggled(object sender, ToggledEventArgs e)
    {
        _filterActive = e.Value;

        if (_filterActive && _currentUserId > 0)
        {
            var userIngredientIds = await _supa.GetUserIngredientIdsAsync(_currentUserId);

            if (userIngredientIds.Count == 0)
            {
                await DisplayAlert("No Ingredients Selected",
                    "Please select your available ingredients first by going to 'View Ingredients' page.",
                    "OK");
                FilterToggle.IsToggled = false;
                _filterActive = false;
                return;
            }
        }

        await LoadRecipes();
    }

    private async void OnFavoritesToggled(object sender, ToggledEventArgs e)
    {
        _favoritesOnly = e.Value;
        await LoadRecipes();
    }

    private async void OnCategoryFilterChanged(object sender, EventArgs e)
        => await LoadRecipes();

    private async Task LoadRecipes()
    {
        var recipes = await _supa.GetAllRecipesAsync();
        var recipeIngredients = await _supa.GetAllRecipeIngredientsAsync();
        var ingredients = await _supa.GetAllIngredientsAsync();

        var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.Name);

        List<int> userIngredientIds = new();
        if (_currentUserId > 0)
        {
            userIngredientIds = await _supa.GetUserIngredientIdsAsync(_currentUserId);
            _userFavoriteRecipeIds = await _supa.GetFavoriteRecipeIdsAsync(_currentUserId);
        }

        IEnumerable<Recipe> filteredRecipes = recipes;

        if (_favoritesOnly && _currentUserId > 0)
        {
            filteredRecipes = filteredRecipes.Where(r => _userFavoriteRecipeIds.Contains(r.Id));
        }

        if (_filterActive && userIngredientIds.Count > 0)
        {
            filteredRecipes = filteredRecipes.Where(r =>
            {
                var recipeIngredientIds = recipeIngredients
                    .Where(ri => ri.RecipeId == r.Id)
                    .Select(ri => ri.IngredientId)
                    .ToHashSet();

                return recipeIngredientIds.All(id => userIngredientIds.Contains(id));
            });
        }

        var selectedCategory = CategoryFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All")
            filteredRecipes = filteredRecipes.Where(r => r.Type == selectedCategory);

        if (!string.IsNullOrWhiteSpace(_searchText))
            filteredRecipes = filteredRecipes.Where(r =>
                r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

        var displayList = filteredRecipes.Select(r =>
        {
            var names = recipeIngredients
                .Where(ri => ri.RecipeId == r.Id)
                .Select(ri => ingredientMap.TryGetValue(ri.IngredientId, out var name)
                    ? name
                    : "(missing ingredient)");

            string icon = r.Type switch
            {
                "Breakfast" => "🍳",
                "Lunch" => "🍔",
                "Dinner" => "🍽️",
                _ => "🍲"
            };

            bool isFav = _userFavoriteRecipeIds.Contains(r.Id);

            return new RecipeDisplay
            {
                Id = r.Id,
                Name = r.Name,
                Type = r.Type,
                Description = r.Description,
                IngredientNames = string.Join(", ", names),
                Icon = icon,
                IsFavorite = isFav,
                FavoriteIcon = isFav ? "⭐" : "☆" // Filled vs Outline star
            };
        }).ToList();

        RecipeList.ItemsSource = displayList;

        if (_filterActive && displayList.Count == 0 && !_favoritesOnly)
        {
            await DisplayAlert("No Recipes Found",
                "No recipes can be made with your current ingredients. Try adding more ingredients to your collection.",
                "OK");
        }
        else if (_favoritesOnly && displayList.Count == 0 && string.IsNullOrWhiteSpace(_searchText))
        {
            await DisplayAlert("No Favorites Yet",
                "You haven't added any favorite recipes yet.",
                "OK");
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? "";
        await LoadRecipes();
    }

    private async void OnRecipeTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int recipeId)
            await Shell.Current.GoToAsync($"recipe-detail?recipeId={recipeId}");
    }

    private async void OnFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (_currentUserId == 0)
        {
            await DisplayAlert("Login Required", "Please login to save favorite recipes.", "OK");
            return;
        }

        if (e.Parameter is int recipeId)
        {
            bool isFav = _userFavoriteRecipeIds.Contains(recipeId);

            if (isFav)
            {
                await _supa.RemoveFavoriteRecipeAsync(_currentUserId, recipeId);
                _userFavoriteRecipeIds.Remove(recipeId);
            }
            else
            {
                await _supa.AddFavoriteRecipeAsync(_currentUserId, recipeId);
                _userFavoriteRecipeIds.Add(recipeId);
            }

            // Immediately refresh list to reflect new favorite state
            await LoadRecipes();
        }
    }

    public class RecipeDisplay : Recipe
    {
        public string IngredientNames { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public string FavoriteIcon { get; set; } = "☆";
    }
}
