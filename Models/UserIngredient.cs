using SQLite;

namespace HoundsBite.Models;

public class UserIngredient
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int IngredientId { get; set; }
}
