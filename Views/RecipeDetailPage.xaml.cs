using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

[QueryProperty(nameof(RecipeId), "recipeId")]
public partial class RecipeDetailPage : ContentPage
{
    private readonly DatabaseService _db;
    private int _recipeId;

    public int RecipeId
    {
        get => _recipeId;
        set
        {
            _recipeId = value;
            LoadRecipeDetails();
        }
    }

    public RecipeDetailPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void LoadRecipeDetails()
    {
        if (_recipeId == 0) return;

        try
        {
            // Load recipe
            var recipe = await _db.Connection.Table<Recipe>()
                .FirstOrDefaultAsync(r => r.Id == _recipeId);

            if (recipe == null)
            {
                await DisplayAlert("Error", "Recipe not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Set basic info
            RecipeName.Text = recipe.Name;
            RecipeType.Text = recipe.Type;
            RecipeDescription.Text = recipe.Description;

            // Set type icon
            TypeIcon.Text = recipe.Type switch
            {
                "Breakfast" => "🍳",
                "Lunch" => "🍔",
                "Dinner" => "🍽️",
                _ => "🍲"
            };

            // Set timing and servings
            PrepTimeLabel.Text = FormatTime(recipe.PrepTime);
            CookTimeLabel.Text = FormatTime(recipe.CookTime);
            ServingsLabel.Text = recipe.Servings.ToString();

            // Set difficulty
            DifficultyLabel.Text = recipe.Difficulty ?? "Medium";
            DifficultyIcon.Text = (recipe.Difficulty ?? "Medium") switch
            {
                "Easy" => "✅",
                "Hard" => "🔴",
                _ => "⚠️"
            };

            // Load ingredients with quantities
            var recipeIngredients = await _db.Connection.Table<RecipeIngredient>()
                .Where(ri => ri.RecipeId == _recipeId)
                .ToListAsync();

            var ingredients = await _db.Connection.Table<Ingredient>().ToListAsync();
            var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.Name);

            var ingredientDisplayList = recipeIngredients.Select(ri =>
            {
                var name = ingredientMap.TryGetValue(ri.IngredientId, out var n) ? n : "(missing)";
                var amountWithUnit = "";
                
                if (!string.IsNullOrWhiteSpace(ri.Amount))
                {
                    amountWithUnit = ri.Amount;
                    if (!string.IsNullOrWhiteSpace(ri.Unit))
                    {
                        amountWithUnit += " " + ri.Unit;
                    }
                }

                return new IngredientDisplay
                {
                    Name = name,
                    AmountWithUnit = amountWithUnit
                };
            }).ToList();

            IngredientsList.ItemsSource = ingredientDisplayList;

            // Set instructions
            InstructionsLabel.Text = string.IsNullOrWhiteSpace(recipe.Instructions)
                ? "No instructions provided yet."
                : recipe.Instructions;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load recipe: {ex.Message}", "OK");
        }
    }

    private string FormatTime(int minutes)
    {
        if (minutes == 0) return "—";
        if (minutes < 60) return $"{minutes}m";
        
        var hours = minutes / 60;
        var mins = minutes % 60;
        return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    public class IngredientDisplay
    {
        public string Name { get; set; }
        public string AmountWithUnit { get; set; }
    }
}
