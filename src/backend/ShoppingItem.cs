using handleliste;

public class ShoppingItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsImage { get; set; } = false;
    public ImageMessage? Image { get; set; }
}