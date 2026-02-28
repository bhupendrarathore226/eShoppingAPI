using Basket.Infrastructure.Data;
using Common.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Basket.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Required for gRPC over plain HTTP (h2c) — used when connecting to Discount.API
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var host = CreateHostBuilder(args).Build();

        // Auto-migrate on startup
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BasketContext>();
            await context.Database.MigrateAsync();
        }

        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)=>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            }).UseSerilog(Logging.ConfigureLogger);
}
