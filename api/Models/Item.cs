public class Item
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int Stock { get; set; }
    public string[]? SearchTerms { get; set; }
    public Store Store {get; set; } = null!;
    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<Tag>? Tags { get; set; } = new List<Tag>();
}