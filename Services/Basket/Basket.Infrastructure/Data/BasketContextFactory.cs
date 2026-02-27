using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Basket.Infrastructure.Data;

public class BasketContextFactory : IDesignTimeDbContextFactory<BasketContext>
{
    public BasketContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BasketContext>();
        var connectionString = "Server=localhost;Port=3306;Database=BasketDb;User=root;Password=4513;";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new BasketContext(optionsBuilder.Options);
    }
}
