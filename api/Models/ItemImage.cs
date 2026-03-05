using Microsoft.EntityFrameworkCore.Storage;

public class ItemImage
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Order { get; set; }
}