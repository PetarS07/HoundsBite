using System.Text.Json.Serialization;

namespace HoundsBite.Models;

public class Ingredient
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("calories")]
    public double? Calories { get; set; }

    [JsonPropertyName("protein")]
    public double? Protein { get; set; }

    [JsonPropertyName("carbs")]
    public double? Carbs { get; set; }

    [JsonPropertyName("fat")]
    public double? Fat { get; set; }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
}