using System.Text.Json.Serialization;

namespace handleliste.Models;

public class GoogleClientSecret
{
    [JsonPropertyName("web")]
    public WebClientInfo? Web { get; set; }
}

public class WebClientInfo
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }
}
