using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //ONE user has ONE credential (password)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Credential)
            .WithOne(c => c.User)
            .HasForeignKey<UserCredential>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //Usernames are unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        //Emails are unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        //Item has ONE store Id, Stores can have MANY items
        modelBuilder.Entity<Item>()
            .HasOne(i => i.Store)
            .WithMany(s => s.Items)                
            .HasForeignKey(i => i.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        //A store can't have multiple items with the same name
        modelBuilder
            .Entity<Item>()
            .HasIndex(i => new { i.StoreId, i.Name })
            .IsUnique();

        //Store names are unique
        modelBuilder.Entity<Store>()
            .HasIndex(s => s.Name)
            .IsUnique();

        //Items have many item images, when deleted the itemId is set to null (for later deletion)
        modelBuilder.Entity<ItemImage>()
            .HasOne<Item>()
            .WithMany(i => i.Images)
            .HasForeignKey(img => img.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    //Database tables
    public DbSet<User> Users { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<ItemImage> ItemImages { get; set;}
    public DbSet<Tag> Tags { get; set; }
}