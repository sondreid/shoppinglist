using Microsoft.EntityFrameworkCore;
using handleliste;
using handleliste.Models;

public class ShoppingItemDB : DbContext
{
    public ShoppingItemDB(DbContextOptions<ShoppingItemDB> options)
        : base(options) { }

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<DinnerPlan> DinnerPlans => Set<DinnerPlan>();
    public DbSet<DinnerIngredient> DinnerIngredients => Set<DinnerIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingItem>().OwnsOne(s => s.Image);
        modelBuilder.Entity<DinnerPlan>().HasIndex(p => p.Date).IsUnique();
    }

    public void EnsureDinnerTables()
    {
        Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DinnerPlans" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DinnerPlans" PRIMARY KEY AUTOINCREMENT,
                "Date" TEXT NOT NULL,
                "Recipe" TEXT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "DinnerIngredients" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DinnerIngredients" PRIMARY KEY AUTOINCREMENT,
                "DinnerPlanId" INTEGER NOT NULL,
                "ShoppingItemId" INTEGER NOT NULL,
                "Name" TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DinnerPlans_Date" ON "DinnerPlans" ("Date");
            """);
    }
}