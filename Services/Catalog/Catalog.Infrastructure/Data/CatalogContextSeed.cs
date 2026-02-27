using System.Text.Json;
using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data;

public class CatalogContextSeed
{
    public static async Task SeedDataAsync(CatalogContext context)
    {
        if (!await context.Products.AnyAsync())
        {
            string path = Path.Combine("Data", "SeedData", "products.json");
            var productsData = await File.ReadAllTextAsync(path);
            var productModels = JsonSerializer.Deserialize<List<ProductSeedModel>>(productsData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (productModels != null)
            {
                var products = productModels.Select(p => new Product
                {
                    Id = p.Id ?? Guid.NewGuid().ToString(),
                    Name = p.Name,
                    Description = p.Description,
                    Summary = p.Summary,
                    ImageFile = p.ImageFile,
                    Price = p.Price,
                    BrandId = p.Brands?.Id,
                    TypeId = p.Types?.Id
                }).ToList();
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }
    }

    private class ProductSeedModel
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Summary { get; set; }
        public string? ImageFile { get; set; }
        public decimal Price { get; set; }
        public RefModel? Brands { get; set; }
        public RefModel? Types { get; set; }
    }

    private class RefModel
    {
        public string? Id { get; set; }
    }
}