namespace handleliste;

public class ImageMessage {
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[]? ImageBinary { get; set; }
}