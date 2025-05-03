using Microsoft.EntityFrameworkCore;

class ShoppingItemDB : DbContext
{
    public ShoppingItemDB(DbContextOptions<ShoppingItemDB> options)
        : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
}