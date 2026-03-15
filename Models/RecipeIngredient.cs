using System.Text.Json.Serialization;

namespace HoundsBite.Models;

public class RecipeIngredient
{
    public int Id { get; set; }

    [JsonPropertyName("recipe_id")]
    public int RecipeId { get; set; }

    [JsonPropertyName("ingredient_id")]
    public int IngredientId { get; set; }

    public string? Amount { get; set; }
    public string? Unit { get; set; }
}