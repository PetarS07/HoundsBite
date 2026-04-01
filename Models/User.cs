using System.Text.Json.Serialization;

namespace HoundsBite.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "User";

    [JsonIgnore]
    public bool IsAdmin => 
        string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase) || 
        string.Equals(Role, "Owner", StringComparison.OrdinalIgnoreCase);

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
