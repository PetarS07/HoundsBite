using HoundsBite.Services;
using HoundsBite.Models;

namespace HoundsBite.Views;

[QueryProperty(nameof(RecipeId), "recipeId")]
public partial class RecipeDetailPage : ContentPage
{
    private readonly SupabaseService _supa;
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

    public RecipeDetailPage(SupabaseService supa)
    {
        InitializeComponent();
        _supa = supa;
    }

    private async void LoadRecipeDetails()
    {
        if (_recipeId == 0) return;

        try
        {
            var recipe = await _supa.GetRecipeByIdAsync(_recipeId);

            if (recipe == null)
            {
                await DisplayAlert("Error", "Recipe not found.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            RecipeName.Text = recipe.Name;
            RecipeType.Text = recipe.Type;
            RecipeDescription.Text = recipe.Description;

            TypeIcon.Text = recipe.Type switch
            {
                "Breakfast" => "🍳",
                "Lunch" => "🍔",
                "Dinner" => "🍽️",
                _ => "🍲"
            };

            PrepTimeLabel.Text = FormatTime(recipe.PrepTime);
            CookTimeLabel.Text = FormatTime(recipe.CookTime);
            ServingsLabel.Text = recipe.Servings.ToString();

            DifficultyLabel.Text = recipe.Difficulty ?? "Medium";
            DifficultyIcon.Text = (recipe.Difficulty ?? "Medium") switch
            {
                "Easy" => "✅",
                "Hard" => "🔴",
                _ => "⚠️"
            };

            int userId = Preferences.Get("LoggedUserId", 0);
            if (userId > 0)
            {
                FavoriteIcon.IsVisible = true;
                var favs = await _supa.GetFavoriteRecipeIdsAsync(userId);
                FavoriteIcon.Text = favs.Contains(_recipeId) ? "⭐" : "☆";
            }
            else
            {
                FavoriteIcon.IsVisible = false;
            }

            var recipeIngredients = await _supa.GetRecipeIngredientsByRecipeIdAsync(_recipeId);
            var ingredients = await _supa.GetAllIngredientsAsync();
            var ingredientMap = ingredients.ToDictionary(i => i.Id, i => i.Name);

            var ingredientDisplayList = recipeIngredients.Select(ri =>
            {
                var name = ingredientMap.TryGetValue(ri.IngredientId, out var n) ? n : "(missing)";
                var amountWithUnit = "";

                if (!string.IsNullOrWhiteSpace(ri.Amount))
                {
                    amountWithUnit = ri.Amount;
                    if (!string.IsNullOrWhiteSpace(ri.Unit))
                        amountWithUnit += " " + ri.Unit;
                }

                return new IngredientDisplay { Name = name, AmountWithUnit = amountWithUnit };
            }).ToList();

            IngredientsList.ItemsSource = ingredientDisplayList;

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
        => await Shell.Current.GoToAsync("..");

    private async void OnFavoriteTapped(object sender, TappedEventArgs e)
    {
        int userId = Preferences.Get("LoggedUserId", 0);
        if (userId == 0) return;

        bool isFav = FavoriteIcon.Text == "⭐";

        if (isFav)
        {
            await _supa.RemoveFavoriteRecipeAsync(userId, _recipeId);
            FavoriteIcon.Text = "☆";
        }
        else
        {
            await _supa.AddFavoriteRecipeAsync(userId, _recipeId);
            FavoriteIcon.Text = "⭐";
        }
    }

    public class IngredientDisplay
    {
        public string Name { get; set; } = string.Empty;
        public string AmountWithUnit { get; set; } = string.Empty;
    }
}
