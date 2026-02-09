using SQLite;

namespace HoundsBite.Models;

public class RecipeIngredient
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    
    // Ingredient quantity details
    public string Amount { get; set; } // e.g., "2", "1.5", "1/4"
    public string Unit { get; set; } // e.g., "cups", "tbsp", "grams"
}