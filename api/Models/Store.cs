public class Store
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool? HasLogo { get; set; }
    public string? Description { get; set; }
}