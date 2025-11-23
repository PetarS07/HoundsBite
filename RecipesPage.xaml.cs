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

        // Build list with ingredient names included
        var displayList = recipes.Select(r => new
        {
            r.Id,
            r.Name,
            r.Type,
            r.Description,
            IngredientNames = string.Join(", ",
                recipeIngredients
                    .Where(ri => ri.RecipeId == r.Id)
                    .Select(ri => ingredients.First(i => i.Id == ri.IngredientId).Name)
            )
        }).ToList();

        RecipeList.ItemsSource = displayList;
    }

    private async void OnDeleteRecipe(object sender, EventArgs e)
    {
        var recipe = (Recipe)((Button)sender).CommandParameter;
        await _db.Connection.DeleteAsync(recipe);
        RecipeList.ItemsSource = await _db.Connection.Table<Recipe>().ToListAsync();
    }
}