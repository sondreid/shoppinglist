using System.Text.Json.Serialization;

namespace handleliste.Models;

public class AuthResponse
{
    [JsonPropertyName("user")]
    public UserInfo User { get; set; } = new();

    [JsonPropertyName("sessionToken")]
    public string SessionToken { get; set; } = string.Empty;
}

public class UserInfo
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

public class AllowedUsersConfig
{
    [JsonPropertyName("emails")]
    public List<string> Emails { get; set; } = new();
}
