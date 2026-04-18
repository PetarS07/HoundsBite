using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace HoundsBite.Services;

public class FoodApiService
{
    private readonly HttpClient _http;
    private const string ApiKey = "bf2d38d762eb4624a8ce43d32c423382";
    private const string BaseUrl = "https://api.spoonacular.com";

    public FoodApiService()
    {
        _http = new HttpClient();
    }

    /// <summary>
    /// Searches the Spoonacular API for ingredients matching the query.
    /// </summary>
    public async Task<List<SpoonacularIngredientSearchResult>> SearchIngredientsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();

        var url = $"{BaseUrl}/food/ingredients/search?apiKey={ApiKey}&query={Uri.EscapeDataString(query)}&number=15";
        
        try
        {
            var response = await _http.GetFromJsonAsync<SpoonacularSearchResponse>(url);
            return response?.Results ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    /// <summary>
    /// Fetches detailed nutritional and categorical information about a specific ingredient.
    /// </summary>
    public async Task<SpoonacularIngredientInformation?> GetIngredientInformationAsync(int externalId)
    {
        var url = $"{BaseUrl}/food/ingredients/{externalId}/information?apiKey={ApiKey}&amount=100&unit=grams";
        
        try
        {
            return await _http.GetFromJsonAsync<SpoonacularIngredientInformation>(url);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Searches the Spoonacular API for recipes matching the query.
    /// </summary>
    public async Task<List<SpoonacularRecipeSearchResult>> SearchRecipesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();

        var url = $"{BaseUrl}/recipes/complexSearch?apiKey={ApiKey}&query={Uri.EscapeDataString(query)}&number=15";
        
        try
        {
            var response = await _http.GetFromJsonAsync<SpoonacularRecipeSearchResponse>(url);
            return response?.Results ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    /// <summary>
    /// Fetches detailed instructions and ingredients about a specific recipe.
    /// </summary>
    public async Task<SpoonacularRecipeInformation?> GetRecipeInformationAsync(int externalId)
    {
        var url = $"{BaseUrl}/recipes/{externalId}/information?apiKey={ApiKey}";
        
        try
        {
            return await _http.GetFromJsonAsync<SpoonacularRecipeInformation>(url);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

// ───────────── API RESPONSE MODELS ─────────────

public class SpoonacularSearchResponse
{
    public List<SpoonacularIngredientSearchResult> Results { get; set; } = new();
}

public class SpoonacularIngredientSearchResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    
    // Helper to generate the full CDN URL for the image
    public string ImageUrl => string.IsNullOrWhiteSpace(Image) 
        ? "" 
        : $"https://img.spoonacular.com/ingredients_100x100/{Image}";
}

public class SpoonacularIngredientInformation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Aisle { get; set; } = string.Empty;
    public NutritionInfo? Nutrition { get; set; }
    public string Image { get; set; } = string.Empty;

    public string ImageUrl => string.IsNullOrWhiteSpace(Image) 
        ? "" 
        : $"https://img.spoonacular.com/ingredients_100x100/{Image}";

    public class NutritionInfo
    {
        public List<Nutrient> Nutrients { get; set; } = new();
    }

    public class Nutrient
    {
        public string Name { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    // Helper properties to easily extract core macros
    public double? Calories => Nutrition?.Nutrients.FirstOrDefault(n => n.Name == "Calories")?.Amount;
    public double? Protein => Nutrition?.Nutrients.FirstOrDefault(n => n.Name == "Protein")?.Amount;
    public double? Carbs => Nutrition?.Nutrients.FirstOrDefault(n => n.Name == "Carbohydrates")?.Amount;
    public double? Fat => Nutrition?.Nutrients.FirstOrDefault(n => n.Name == "Fat")?.Amount;
}

public class SpoonacularRecipeSearchResponse
{
    public List<SpoonacularRecipeSearchResult> Results { get; set; } = new();
}

public class SpoonacularRecipeSearchResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}

public class SpoonacularRecipeInformation
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("readyInMinutes")]
    public int ReadyInMinutes { get; set; }
    public int Servings { get; set; }
    
    [JsonPropertyName("extendedIngredients")]
    public List<SpoonacularRecipeIngredient> ExtendedIngredients { get; set; } = new();
}

public class SpoonacularRecipeIngredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    
    // Spoonacular returns basic aisle info here too, which is great for auto-creating ingredients
    [JsonPropertyName("aisle")]
    public string? Aisle { get; set; }
}
