using System.Text.Json.Serialization;

namespace HoundsBite.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }

    public string? Instructions { get; set; }

    [JsonPropertyName("prep_time")]
    public int? PrepTime { get; set; }

    [JsonPropertyName("cook_time")]
    public int? CookTime { get; set; }

    public int? Servings { get; set; } = 1;
    public string? Difficulty { get; set; } = "Medium";

    [JsonPropertyName("image_path")]
    public string? ImagePath { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "Community";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Approved";

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);
}