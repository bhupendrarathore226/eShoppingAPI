using System.Diagnostics;
using Catalog.Infrastructure.Data;
using Common.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Catalog.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        var host = CreateHostBuilder(args).Build();

        // Auto-migrate and seed on startup
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
            await context.Database.MigrateAsync();
            await BrandContextSeed.SeedDataAsync(context);
            await TypeContextSeed.SeedDataAsync(context);
            await CatalogContextSeed.SeedDataAsync(context);
        }

        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            }).UseSerilog(Logging.ConfigureLogger);
}