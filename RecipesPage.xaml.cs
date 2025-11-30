using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite;

public partial class RecipesPage : ContentPage
{
    private readonly DatabaseService _db;

    public class IngredientCheck
    {
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }

    private List<IngredientCheck> _ingredientChecks = new();

    public RecipesPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load ingredients for selection
        var ingredients = await _db.Connection.Table<Ingredient>().ToListAsync();

        _ingredientChecks = ingredients.Select(i => new IngredientCheck
        {
            IngredientId = i.Id,
            Name = i.Name,
            IsSelected = false
        }).ToList();

        IngredientSelector.ItemsSource = _ingredientChecks;

        // Load recipes list (with ingredient names)
        await LoadRecipes();
    }

    private async void OnAddRecipe(object sender, EventArgs e)
    {
        var newRecipe = new Recipe
        {
            Name = RecipeName.Text,
            Type = RecipeType.Text,
            Description = RecipeDesc.Text
        };

        await _db.Connection.InsertAsync(newRecipe);

        // Save selected ingredients
        var selectedIngredients = _ingredientChecks.Where(x => x.IsSelected).ToList();

        foreach (var ing in selectedIngredients)
        {
            await _db.Connection.InsertAsync(new RecipeIngredient
            {
                RecipeId = newRecipe.Id,
                IngredientId = ing.IngredientId
            });
        }

        await LoadRecipes();

        RecipeName.Text = "";
        RecipeType.Text = "";
        RecipeDesc.Text = "";

        // Reset selection
        foreach (var x in _ingredientChecks) x.IsSelected = false;
        IngredientSelector.ItemsSource = null;
        IngredientSelector.ItemsSource = _ingredientChecks;
    }
    private async Task LoadRecipes()
    {
        var recipes = await _db.Connection.Table<Recipe>().ToListAsync();
        var recipeIngredients = await _db.Connection.Table<RecipeIngredient>().ToListAsync();
        var ingredients = await _db.Connection.Table<Ingredient>().ToListAsync();

        // Build quick lookup: IngredientId -> Name
        var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.Name);

        var displayList = recipes.Select(r =>
        {
            var names = recipeIngredients
                .Where(ri => ri.RecipeId == r.Id)
                .Select(ri =>
                {
                    // Safe lookup
                    return ingredientMap.TryGetValue(ri.IngredientId, out var name)
                        ? name
                        : "(missing ingredient)";
                });

            return new
            {
                r.Id,
                r.Name,
                r.Type,
                r.Description,
                IngredientNames = string.Join(", ", names)
            };
        }).ToList();

        RecipeList.ItemsSource = displayList;
    }

    private async void OnDeleteRecipe(object sender, EventArgs e)
    {
        var recipe = (Recipe)((Button)sender).CommandParameter;

        // delete links first
        var links = await _db.Connection.Table<RecipeIngredient>()
            .Where(x => x.RecipeId == recipe.Id)
            .ToListAsync();

        foreach (var link in links)
            await _db.Connection.DeleteAsync(link);

        // delete recipe
        await _db.Connection.DeleteAsync(recipe);

        await LoadRecipes();
    }
}