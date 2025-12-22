using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class RecipesPage : ContentPage
{
    private readonly DatabaseService _db;
    private Recipe _editingRecipe = null;

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

    private async void OnEditRecipe(object sender, EventArgs e)
    {
        var display = (RecipeDisplay)((Button)sender).CommandParameter;

        _editingRecipe = await _db.Connection.Table<Recipe>()
            .FirstAsync(r => r.Id == display.Id);

        RecipeName.Text = _editingRecipe.Name;
        RecipeType.SelectedItem = _editingRecipe.Type;
        RecipeDesc.Text = _editingRecipe.Description;

        // Load ingredient links
        var used = await _db.Connection.Table<RecipeIngredient>()
            .Where(x => x.RecipeId == _editingRecipe.Id)
            .ToListAsync();

        foreach (var box in _ingredientChecks)
            box.IsSelected = used.Any(u => u.IngredientId == box.IngredientId);

        IngredientSelector.ItemsSource = null;
        IngredientSelector.ItemsSource = _ingredientChecks;
    }

    private async void OnAddRecipe(object sender, EventArgs e)
    {
        if (_editingRecipe == null)
        {
            var newRecipe = new Recipe
            {
                Name = RecipeName.Text,
                Type = RecipeType.SelectedItem?.ToString() ?? "Other",
                Description = RecipeDesc.Text,
                UserId = Preferences.Get("LoggedUserId", 0)
            };

            await _db.Connection.InsertAsync(newRecipe);
            _editingRecipe = newRecipe;
        }
        else
        {
            _editingRecipe.Name = RecipeName.Text;
            _editingRecipe.Type = RecipeType.SelectedItem?.ToString() ?? "Other";
            _editingRecipe.Description = RecipeDesc.Text;

            await _db.Connection.UpdateAsync(_editingRecipe);
        }

        // Update ingredients
        var links = await _db.Connection.Table<RecipeIngredient>()
            .Where(x => x.RecipeId == _editingRecipe.Id)
            .ToListAsync();

        foreach (var l in links)
            await _db.Connection.DeleteAsync(l);

        foreach (var ing in _ingredientChecks.Where(i => i.IsSelected))
        {
            await _db.Connection.InsertAsync(new RecipeIngredient
            {
                RecipeId = _editingRecipe.Id,
                IngredientId = ing.IngredientId
            });
        }

        ClearRecipeForm();
        _editingRecipe = null;

        await LoadRecipes();
    }
    private void ClearRecipeForm()
    {
        RecipeName.Text = "";
        RecipeType.SelectedIndex = -1;
        RecipeDesc.Text = "";

        foreach (var item in _ingredientChecks)
            item.IsSelected = false;

        IngredientSelector.ItemsSource = null;
        IngredientSelector.ItemsSource = _ingredientChecks;
    }
    private async Task LoadRecipes()
    {
        var recipes = await _db.Connection.Table<Recipe>().ToListAsync();
        var recipeIngredients = await _db.Connection.Table<RecipeIngredient>().ToListAsync();
        var ingredients = await _db.Connection.Table<Ingredient>().ToListAsync();

        var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.Name);

        var displayList = recipes.Select(r =>
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
    }

    private async void OnDeleteRecipe(object sender, EventArgs e)
    {
        var recipeDisplay = (RecipeDisplay)((Button)sender).CommandParameter;

        // now get real recipe
        var recipe = await _db.Connection.Table<Recipe>()
            .FirstAsync(r => r.Id == recipeDisplay.Id);

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
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }
}