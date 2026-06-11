public class DinnerPlan
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd, one plan per day
    public string? Recipe { get; set; }
    public DateTime UpdatedAt { get; set; }
}
