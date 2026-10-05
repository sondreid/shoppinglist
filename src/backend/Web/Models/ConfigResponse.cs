using System.Text.Json.Serialization;

namespace handleliste.Web.Models;

public class ConfigResponse
{
    [JsonPropertyName("googleClientId")]
    public string? GoogleClientId { get; set; }
}
