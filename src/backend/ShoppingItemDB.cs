using Microsoft.EntityFrameworkCore;
using handleliste;

class ShoppingItemDB : DbContext
{
    public ShoppingItemDB(DbContextOptions<ShoppingItemDB> options)
        : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingItem>().OwnsOne(s => s.Image);
    }
}