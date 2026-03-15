using System.Text.Json.Serialization;

namespace HoundsBite.Models;

public class FavoriteRecipe
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("recipe_id")]
    public int RecipeId { get; set; }
}
