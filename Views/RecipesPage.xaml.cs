using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

public partial class RecipesPage : ContentPage
{
    private readonly SupabaseService _supa;
    private Recipe? _editingRecipe = null;

    public class IngredientCheck
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string Amount { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    private List<IngredientCheck> _ingredientChecks = new();

    public RecipesPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var ingredients = await _supa.GetAllIngredientsAsync();

        _ingredientChecks = ingredients.Select(i => new IngredientCheck
        {
            IngredientId = i.Id,
            Name = i.Name,
            IsSelected = false,
            Amount = "",
            Unit = ""
        }).ToList();

        IngredientSelector.ItemsSource = _ingredientChecks;

        await LoadRecipes();
    }

    private async void OnEditRecipe(object sender, EventArgs e)
    {
        var display = (RecipeDisplay)((Button)sender).CommandParameter;

        _editingRecipe = await _supa.GetRecipeByIdAsync(display.Id);
        if (_editingRecipe == null) return;

        RecipeName.Text = _editingRecipe.Name;
        RecipeType.SelectedItem = _editingRecipe.Type;
        RecipeInstructions.Text = _editingRecipe.Instructions;
        PrepTimeEntry.Text = _editingRecipe.PrepTime.ToString();
        CookTimeEntry.Text = _editingRecipe.CookTime.ToString();
        ServingsEntry.Text = _editingRecipe.Servings.ToString();
        DifficultyPicker.SelectedItem = _editingRecipe.Difficulty ?? "Medium";
        RecipeDesc.Text = _editingRecipe.Description;

        var used = await _supa.GetRecipeIngredientsByRecipeIdAsync(_editingRecipe.Id);

        foreach (var box in _ingredientChecks)
        {
            var usedIngredient = used.FirstOrDefault(u => u.IngredientId == box.IngredientId);
            box.IsSelected = usedIngredient != null;
            if (usedIngredient != null)
            {
                box.Amount = usedIngredient.Amount ?? "";
                box.Unit = usedIngredient.Unit ?? "";
            }
        }

        IngredientSelector.ItemsSource = null;
        IngredientSelector.ItemsSource = _ingredientChecks;
    }

    private async void OnAddRecipe(object sender, EventArgs e)
    {
        if (_editingRecipe == null)
        {
            var newRecipe = new Recipe
            {
                Name = RecipeName.Text ?? "",
                Type = RecipeType.SelectedItem?.ToString() ?? "Other",
                Description = RecipeDesc.Text,
                UserId = Preferences.Get("LoggedUserId", 0),
                Instructions = RecipeInstructions.Text ?? "",
                PrepTime = int.TryParse(PrepTimeEntry.Text, out var prep) ? prep : 0,
                CookTime = int.TryParse(CookTimeEntry.Text, out var cook) ? cook : 0,
                Servings = int.TryParse(ServingsEntry.Text, out var servings) ? servings : 1,
                Difficulty = DifficultyPicker.SelectedItem?.ToString() ?? "Medium",
                ImagePath = null
            };

            _editingRecipe = await _supa.AddRecipeAsync(newRecipe);
        }
        else
        {
            _editingRecipe.Name = RecipeName.Text ?? "";
            _editingRecipe.Type = RecipeType.SelectedItem?.ToString() ?? "Other";
            _editingRecipe.Description = RecipeDesc.Text;
            _editingRecipe.Instructions = RecipeInstructions.Text ?? "";
            _editingRecipe.PrepTime = int.TryParse(PrepTimeEntry.Text, out var prep) ? prep : 0;
            _editingRecipe.CookTime = int.TryParse(CookTimeEntry.Text, out var cook) ? cook : 0;
            _editingRecipe.Servings = int.TryParse(ServingsEntry.Text, out var servings) ? servings : 1;
            _editingRecipe.Difficulty = DifficultyPicker.SelectedItem?.ToString() ?? "Medium";

            await _supa.UpdateRecipeAsync(_editingRecipe);
        }

        // Replace recipe ingredients
        await _supa.DeleteRecipeIngredientsByRecipeIdAsync(_editingRecipe.Id);

        foreach (var ing in _ingredientChecks.Where(i => i.IsSelected))
        {
            await _supa.AddRecipeIngredientAsync(new RecipeIngredient
            {
                RecipeId = _editingRecipe.Id,
                IngredientId = ing.IngredientId,
                Amount = ing.Amount ?? "",
                Unit = ing.Unit ?? ""
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
        RecipeInstructions.Text = "";
        PrepTimeEntry.Text = "";
        CookTimeEntry.Text = "";
        ServingsEntry.Text = "";
        DifficultyPicker.SelectedIndex = -1;

        foreach (var item in _ingredientChecks)
        {
            item.IsSelected = false;
            item.Amount = "";
            item.Unit = "";
        }

        IngredientSelector.ItemsSource = null;
        IngredientSelector.ItemsSource = _ingredientChecks;
    }

    private async Task LoadRecipes()
    {
        var recipes = await _supa.GetAllRecipesAsync();
        var recipeIngredients = await _supa.GetAllRecipeIngredientsAsync();
        var ingredients = await _supa.GetAllIngredientsAsync();

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
        await _supa.DeleteRecipeAsync(recipeDisplay.Id);
        await LoadRecipes();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }
}