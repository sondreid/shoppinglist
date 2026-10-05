namespace handleliste.Web.Models;

public class ImageRequest
{
    public required string Base64Image { get; set; }
    public string? ContentType { get; set; }
}