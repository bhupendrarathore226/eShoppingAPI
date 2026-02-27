using Basket.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Basket.Infrastructure.Data;

public class BasketContext : DbContext
{
    public BasketContext(DbContextOptions<BasketContext> options) : base(options) { }

    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingCart>()
            .HasIndex(c => c.UserName)
            .IsUnique();

        modelBuilder.Entity<ShoppingCartItem>()
            .HasOne<ShoppingCart>()
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.ShoppingCartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
