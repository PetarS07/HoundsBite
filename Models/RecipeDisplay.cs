using System;
using System.Collections.Generic;

namespace HoundsBite.Models;

public class RecipeDisplay : Recipe
{
    public string IngredientNames { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public string FavoriteIcon { get; set; } = "☆";
    
    // API Integration Fields
    public bool IsLocalMode { get; set; } = true;
    public bool IsApiMode { get; set; } = false;
}
