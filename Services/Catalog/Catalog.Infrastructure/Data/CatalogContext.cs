using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data;

public class CatalogContext : DbContext, ICatalogContext
{
    public CatalogContext(DbContextOptions<CatalogContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductBrand> Brands { get; set; } = null!;
    public DbSet<ProductType> Types { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brands)
            .WithMany()
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Types)
            .WithMany()
            .HasForeignKey(p => p.TypeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Product>().Property(p => p.Id).HasMaxLength(100);
        modelBuilder.Entity<ProductBrand>().Property(p => p.Id).HasMaxLength(100);
        modelBuilder.Entity<ProductType>().Property(p => p.Id).HasMaxLength(100);
    }
}