using SQLite;

namespace HoundsBite.Models;

public class Recipe
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public int UserId { get; set; }
    
    // Enhanced recipe details
    public string Instructions { get; set; }
    public int PrepTime { get; set; } // in minutes
    public int CookTime { get; set; } // in minutes
    public int Servings { get; set; }
    public string Difficulty { get; set; } // Easy, Medium, Hard
    public string ImagePath { get; set; } // for future enhancement
}