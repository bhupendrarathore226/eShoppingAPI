using Catalog.Core.Entities;
using Catalog.Core.Repositories;
using Catalog.Core.Specs;
using Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public class ProductRepository : IProductRepository, IBrandRepository, ITypesRepository
{
    private readonly ICatalogContext _context;

    public ProductRepository(ICatalogContext context)
    {
        _context = context;
    }

    public async Task<Pagination<Product>> GetProducts(CatalogSpecParams catalogSpecParams)
    {
        var query = _context.Products
            .Include(p => p.Brands)
            .Include(p => p.Types)
            .AsQueryable();

        if (!string.IsNullOrEmpty(catalogSpecParams.Search))
            query = query.Where(p => p.Name.ToLower().Contains(catalogSpecParams.Search.ToLower()));

        if (!string.IsNullOrEmpty(catalogSpecParams.BrandId))
            query = query.Where(p => p.BrandId == catalogSpecParams.BrandId);

        if (!string.IsNullOrEmpty(catalogSpecParams.TypeId))
            query = query.Where(p => p.TypeId == catalogSpecParams.TypeId);

        query = catalogSpecParams.Sort switch
        {
            "priceAsc"  => query.OrderBy(p => p.Price),
            "priceDesc" => query.OrderByDescending(p => p.Price),
            _           => query.OrderBy(p => p.Name)
        };

        var count = await query.CountAsync();
        var data = await query
            .Skip(catalogSpecParams.PageSize * (catalogSpecParams.PageIndex - 1))
            .Take(catalogSpecParams.PageSize)
            .ToListAsync();

        return new Pagination<Product>
        {
            PageSize  = catalogSpecParams.PageSize,
            PageIndex = catalogSpecParams.PageIndex,
            Count     = count,
            Data      = data
        };
    }

    public async Task<Product> GetProduct(string id)
    {
        return (await _context.Products
            .Include(p => p.Brands)
            .Include(p => p.Types)
            .FirstOrDefaultAsync(p => p.Id == id))!;
    }

    public async Task<IEnumerable<Product>> GetProductByName(string name)
    {
        return await _context.Products
            .Include(p => p.Brands)
            .Include(p => p.Types)
            .Where(p => p.Name == name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductByBrand(string name)
    {
        return await _context.Products
            .Include(p => p.Brands)
            .Include(p => p.Types)
            .Where(p => p.Brands != null && p.Brands.Name == name)
            .ToListAsync();
    }

    public async Task<Product> CreateProduct(Product product)
    {
        if (string.IsNullOrEmpty(product.Id))
            product.Id = Guid.NewGuid().ToString();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateProduct(Product product)
    {
        _context.Products.Update(product);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteProduct(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;
        _context.Products.Remove(product);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<ProductBrand>> GetAllBrands()
    {
        return await _context.Brands.ToListAsync();
    }

    public async Task<IEnumerable<ProductType>> GetAllTypes()
    {
        return await _context.Types.ToListAsync();
    }
}