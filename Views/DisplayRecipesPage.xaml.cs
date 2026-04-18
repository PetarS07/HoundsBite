using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class DisplayRecipesPage : ContentPage
{
    private readonly SupabaseService _supa;
    private readonly FoodApiService _foodApi;
    
    private bool _filterActive = false;
    private bool _favoritesOnly = false;
    private int _currentUserId = 0;
    private string _searchText = "";
    private List<int> _userFavoriteRecipeIds = new();

    public DisplayRecipesPage(SupabaseService supa, FoodApiService foodApi)
    {
        InitializeComponent();
        _supa = supa;
        _foodApi = foodApi;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _currentUserId = Preferences.Get("LoggedUserId", 0);
        
        // Refresh Visibility based on mode and login
        UpdateVisibility();

        if (CategoryFilter.SelectedIndex == -1)
            CategoryFilter.SelectedIndex = 0;

        await LoadRecipes();
    }

    private void UpdateVisibility()
    {
        FilterToggleContainer.IsVisible = _currentUserId > 0;
        FavoritesToggleContainer.IsVisible = _currentUserId > 0;
        FiltersPanel.IsVisible = true;
    }

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        await LoadRecipes();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? "";
        // We'll wait for button press for API heavy search, 
        // but can live-filter local if desired. For now, button only to prevent API spam.
    }

    private async Task LoadRecipes()
    {
        ApiLoadingIndicator.IsVisible = true;
        ApiLoadingIndicator.IsRunning = true;

        try
        {
            var combinedDisplayList = new List<RecipeDisplay>();

            // 1. Fetch API Recipes (If searching)
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var apiResults = await _foodApi.SearchRecipesAsync(_searchText);
                combinedDisplayList.AddRange(apiResults.Select(r => new RecipeDisplay
                {
                    ExternalId = r.Id.ToString(),
                    Name = r.Title,
                    ImagePath = r.Image,
                    IsApiMode = true,
                    IsLocalMode = false
                }));
            }

            // 2. Fetch Local Community Recipes
            var localRecipes = await _supa.GetAllRecipesAsync();
            var recipeIngredients = await _supa.GetAllRecipeIngredientsAsync();
            
            List<int> userIngredientIds = new();
            if (_currentUserId > 0)
            {
                userIngredientIds = await _supa.GetUserIngredientIdsAsync(_currentUserId);
                _userFavoriteRecipeIds = await _supa.GetFavoriteRecipeIdsAsync(_currentUserId);
            }

            // Filter Community Recipes (Only Approved ones)
            IEnumerable<Recipe> filteredLocal = localRecipes.Where(r => r.Status == "Approved");

            if (_favoritesOnly && _currentUserId > 0)
                filteredLocal = filteredLocal.Where(r => _userFavoriteRecipeIds.Contains(r.Id));

            if (_filterActive && userIngredientIds.Count > 0)
            {
                filteredLocal = filteredLocal.Where(r =>
                {
                    var riIds = recipeIngredients.Where(ri => ri.RecipeId == r.Id).Select(ri => ri.IngredientId);
                    return riIds.Any() && riIds.All(id => userIngredientIds.Contains(id));
                });
            }

            var selectedCategory = CategoryFilter.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All")
                filteredLocal = filteredLocal.Where(r => r.Type == selectedCategory);

            if (!string.IsNullOrWhiteSpace(_searchText))
                filteredLocal = filteredLocal.Where(r => r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            combinedDisplayList.AddRange(filteredLocal.Select(r => new RecipeDisplay
            {
                Id = r.Id,
                Name = r.Name,
                Type = r.Type,
                ImagePath = r.ImagePath,
                IsFavorite = _userFavoriteRecipeIds.Contains(r.Id),
                FavoriteIcon = _userFavoriteRecipeIds.Contains(r.Id) ? "⭐" : "☆",
                IsLocalMode = true,
                IsApiMode = false
            }));

            RecipeList.ItemsSource = combinedDisplayList;

            if (combinedDisplayList.Count == 0 && !string.IsNullOrWhiteSpace(_searchText))
                await DisplayAlert("No Results", "No recipes found matching your search.", "OK");
        }
        finally
        {
            ApiLoadingIndicator.IsVisible = false;
            ApiLoadingIndicator.IsRunning = false;
        }
    }

    private async void OnImportRecipeClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is RecipeDisplay apiRecipe)
        {
            if (string.IsNullOrEmpty(apiRecipe.ExternalId)) return;

            ApiLoadingIndicator.IsVisible = true;
            ApiLoadingIndicator.IsRunning = true;

            try
            {
                // 1. Check if already imported
                var existing = await _supa.GetRecipeByExternalIdAsync(apiRecipe.ExternalId);
                if (existing != null)
                {
                    await DisplayAlert("Already Imported", $"'{apiRecipe.Name}' is already in the community database.", "View Recipe");
                    await Shell.Current.GoToAsync($"recipe-detail?recipeId={existing.Id}");
                    return;
                }

                // 2. Fetch Deep details
                var details = await _foodApi.GetRecipeInformationAsync(int.Parse(apiRecipe.ExternalId));
                if (details == null)
                {
                    await DisplayAlert("Error", "Could not fetch recipe details from API.", "OK");
                    return;
                }

                // 3. Import logic start
                var newRecipe = new Recipe
                {
                    Name = details.Title,
                    ImagePath = details.Image,
                    Instructions = details.Instructions,
                    SourceUrl = details.SourceUrl,
                    ExternalId = details.Id.ToString(),
                    PrepTime = details.ReadyInMinutes / 2, // Approximating since API gives total
                    CookTime = details.ReadyInMinutes / 2,
                    Servings = details.Servings,
                    Difficulty = "Medium",
                    Type = "Other", // Default
                    UserId = _currentUserId > 0 ? _currentUserId : 1, // Admin fallback
                    Source = "Api",
                    Status = "Approved"
                };

                // Add to DB
                var savedRecipe = await _supa.AddRecipeAsync(newRecipe);

                // 4. Auto-Mapping Ingredients
                var allDbIngredients = await _supa.GetAllIngredientsAsync();
                var recipeIngredientIds = new HashSet<int>();

                foreach (var apiIng in details.ExtendedIngredients)
                {
                    // Match by ExternalId or Name
                    var matched = allDbIngredients.FirstOrDefault(i => 
                        i.ExternalId == apiIng.Id.ToString() || 
                        i.Name.Equals(apiIng.Name, StringComparison.OrdinalIgnoreCase));

                    if (matched == null)
                    {
                        // Create and Cache new ingredient
                        var newIng = new Ingredient
                        {
                            Name = apiIng.Name,
                            ExternalId = apiIng.Id.ToString(),
                            Category = apiIng.Aisle ?? "Other",
                            ImageUrl = string.IsNullOrWhiteSpace(apiIng.Image) ? "" : $"https://img.spoonacular.com/ingredients_100x100/{apiIng.Image}"
                        };
                        matched = await _supa.AddIngredientAsync(newIng);
                        allDbIngredients.Add(matched); // Keep local list updated
                    }

                    recipeIngredientIds.Add(matched.Id);

                    // Link to Recipe
                    await _supa.AddRecipeIngredientAsync(new RecipeIngredient 
                    { 
                        RecipeId = savedRecipe.Id, 
                        IngredientId = matched.Id,
                        Amount = apiIng.Amount.ToString(),
                        Unit = apiIng.Unit ?? ""
                    });
                }

                if (_currentUserId > 0)
                {
                    foreach (var ingId in recipeIngredientIds)
                        await _supa.AddUserIngredientAsync(_currentUserId, ingId);
                }

                var importMsg = _currentUserId > 0
                    ? "Recipe imported. Its ingredients were added to your kitchen inventory."
                    : "Recipe imported successfully! Log in to sync those ingredients to your kitchen.";

                await DisplayAlert("Success", importMsg, "View Now");
                await Shell.Current.GoToAsync($"recipe-detail?recipeId={savedRecipe.Id}");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Import Failed", ex.Message, "OK");
            }
            finally
            {
                ApiLoadingIndicator.IsVisible = false;
                ApiLoadingIndicator.IsRunning = false;
            }
        }
    }

    private async void OnRecipeTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is RecipeDisplay r && r.IsLocalMode)
            await Shell.Current.GoToAsync($"recipe-detail?recipeId={r.Id}");
    }

    private async void OnFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (_currentUserId == 0)
        {
            await DisplayAlert("Login Required", "Please login to save favorite recipes.", "OK");
            return;
        }

        if (e.Parameter is RecipeDisplay r && r.IsLocalMode)
        {
            bool isFav = _userFavoriteRecipeIds.Contains(r.Id);

            if (isFav)
            {
                await _supa.RemoveFavoriteRecipeAsync(_currentUserId, r.Id);
                _userFavoriteRecipeIds.Remove(r.Id);
            }
            else
            {
                await _supa.AddFavoriteRecipeAsync(_currentUserId, r.Id);
                _userFavoriteRecipeIds.Add(r.Id);
            }

            await LoadRecipes();
        }
    }

    private async void OnFilterToggled(object sender, ToggledEventArgs e)
    {
        _filterActive = e.Value;
        if (_filterActive && _currentUserId > 0)
        {
            var userIngs = await _supa.GetUserIngredientIdsAsync(_currentUserId);
            if (userIngs.Count == 0)
            {
                await DisplayAlert("No Ingredients", "Please select your kitchen items first.", "OK");
                FilterToggle.IsToggled = false;
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
    {
        await LoadRecipes();
    }
}
