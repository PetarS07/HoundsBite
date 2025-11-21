using SQLite;

namespace HoundsBite.Models;

public class RecipeIngredient
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
}