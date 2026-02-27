using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data;

public interface ICatalogContext
{
    DbSet<Product> Products { get; }
    DbSet<ProductBrand> Brands { get; }
    DbSet<ProductType> Types { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}