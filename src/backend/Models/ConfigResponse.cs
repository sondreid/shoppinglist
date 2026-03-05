using System.Text.Json.Serialization;

namespace handleliste.Models;

public class ConfigResponse
{
    [JsonPropertyName("googleClientId")]
    public string? GoogleClientId { get; set; }
}
