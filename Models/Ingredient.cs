using SQLite;

namespace HoundsBite.Models;

public class Ingredient
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
}