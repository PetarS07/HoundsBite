using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class DisplayRecipesPage : ContentPage
{
    private readonly DatabaseService _db;
    private bool _filterActive = false;
    private int _currentUserId = 0;

    public DisplayRecipesPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check login status and show/hide filter toggle
        _currentUserId = Preferences.Get("LoggedUserId", 0);
        FilterToggleContainer.IsVisible = _currentUserId > 0;
        
        // Set default category filter to "All"
        if (CategoryFilter.SelectedIndex == -1)
        {
            CategoryFilter.SelectedIndex = 0; // "All"
        }
        
        await LoadRecipes();
    }

    private async void OnFilterToggled(object sender, ToggledEventArgs e)
    {
        _filterActive = e.Value;
        
        // Check if user has selected ingredients when enabling filter
        if (_filterActive && _currentUserId > 0)
        {
            var userIngredientIds = await _db.GetUserIngredientIdsAsync(_currentUserId);
            
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

    private async void OnCategoryFilterChanged(object sender, EventArgs e)
    {
        await LoadRecipes();
    }

    private async Task LoadRecipes()
    {
        var recipes = await _db.Connection.Table<Recipe>().ToListAsync();
        var recipeIngredients = await _db.Connection.Table<RecipeIngredient>().ToListAsync();
        var ingredients = await _db.Connection.Table<Ingredient>().ToListAsync();

        var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.Name);

        // Get user's available ingredients from database
        List<int> userIngredientIds = new();
        if (_currentUserId > 0)
        {
            userIngredientIds = await _db.GetUserIngredientIdsAsync(_currentUserId);
        }

        // Filter recipes based on toggle state
        IEnumerable<Recipe> filteredRecipes = recipes;
        
        if (_filterActive && userIngredientIds.Count > 0)
        {
            // Show only recipes where user has ALL required ingredients
            filteredRecipes = recipes.Where(r =>
            {
                var recipeIngredientIds = recipeIngredients
                    .Where(ri => ri.RecipeId == r.Id)
                    .Select(ri => ri.IngredientId)
                    .ToHashSet();

                // Recipe can be made if all its ingredients are in user's collection
                return recipeIngredientIds.All(ingredientId => userIngredientIds.Contains(ingredientId));
            });
        }

        // Filter by category
        var selectedCategory = CategoryFilter.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All")
        {
            filteredRecipes = filteredRecipes.Where(r => r.Type == selectedCategory);
        }

        var displayList = filteredRecipes.Select(r =>
        {
            var names = recipeIngredients
                .Where(ri => ri.RecipeId == r.Id)
                .Select(ri => ingredientMap.TryGetValue(ri.IngredientId, out var name)
                    ? name
                    : "(missing ingredient)");

            return new RecipeDisplay
            {
                Id = r.Id,
                Name = r.Name,
                Type = r.Type,
                Description = r.Description,
                IngredientNames = string.Join(", ", names)
            };
        }).ToList();

        RecipeList.ItemsSource = displayList;
        
        // Show message if filter is active and no recipes found
        if (_filterActive && displayList.Count == 0)
        {
            await DisplayAlert("No Recipes Found", 
                "No recipes can be made with your current ingredients. Try adding more ingredients to your collection.", 
                "OK");
        }
    }
}
