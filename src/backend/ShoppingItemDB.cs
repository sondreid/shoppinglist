using Microsoft.EntityFrameworkCore;
using handleliste;
using handleliste.Models;

public class ShoppingItemDB : DbContext
{
    public ShoppingItemDB(DbContextOptions<ShoppingItemDB> options)
        : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingItem>().OwnsOne(s => s.Image);
    }
}