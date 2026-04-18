using System.Text.Json.Serialization;

namespace HoundsBite.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
