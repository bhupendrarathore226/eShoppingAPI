using System.Text.Json;
using Catalog.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Data;

public class TypeContextSeed
{
    public static async Task SeedDataAsync(CatalogContext context)
    {
        if (!await context.Types.AnyAsync())
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "types.json");
            var typesData = await File.ReadAllTextAsync(path);
            var types = JsonSerializer.Deserialize<List<ProductType>>(typesData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (types != null)
            {
                await context.Types.AddRangeAsync(types);
                await context.SaveChangesAsync();
            }
        }
    }
}