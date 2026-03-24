using Microsoft.Extensions.Configuration.UserSecrets;

public class Store
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Description { get; set; }

    public User Owner { get; set; } = null!;
}