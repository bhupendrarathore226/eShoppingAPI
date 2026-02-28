using System.Text.Json;
using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data;

public static class BrandContextSeed
{
    public static async Task SeedDataAsync(CatalogContext context)
    {
        if (!await context.Brands.AnyAsync())
        {
         
            //E:\AI Project\eShoppingAPI\Services\Catalog\Catalog.Infrastructure\Data\SeedData\brands.json
            string path= Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "brands.json");
            //string path = Path.Combine("Data", "SeedData", "brands.json");
            var brandsData = await File.ReadAllTextAsync(path);
            var brands = JsonSerializer.Deserialize<List<ProductBrand>>(brandsData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (brands != null)
            {
                await context.Brands.AddRangeAsync(brands);
                await context.SaveChangesAsync();
            }
        }
    }
}