using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace handleliste.Models;

public class Session
{
    [Key]
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public DateTime ExpiresAt { get; set; }
}
