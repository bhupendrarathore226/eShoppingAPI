using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Discount.Infrastructure.Extensions;

public static class DbExtension
{
    public static IHost MigrateDatabase<TContext>(this IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var config = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<TContext>>();
            try
            {
                logger.LogInformation("Discount DB Migration Started");
                ApplyMigrations(config);
                logger.LogInformation("Discount DB Migration Completed");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return host;
    }

    private static void ApplyMigrations(IConfiguration config)
    {
        var connectionString = config.GetValue<string>("DatabaseSettings:ConnectionString");

        // Strip the Database= segment so we can connect before the DB exists
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        builder.Database = string.Empty;

        using var connection = new MySqlConnection(builder.ConnectionString);
        connection.Open();
        using var cmd = new MySqlCommand()
        {
            Connection = connection
        };

        cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`";
        cmd.ExecuteNonQuery();
        cmd.CommandText = $"USE `{databaseName}`";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS Coupon";
        cmd.ExecuteNonQuery();
        cmd.CommandText = @"CREATE TABLE Coupon(Id INT AUTO_INCREMENT PRIMARY KEY,
                                                ProductName VARCHAR(500) NOT NULL,
                                                Description TEXT,
                                                Amount INT)";
        cmd.ExecuteNonQuery();
        
        cmd.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES('Adidas Quick Force Indoor Badminton Shoes', 'Shoe Discount', 500);";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES('Yonex VCORE Pro 100 A Tennis Racquet (270gm, Strung)', 'Racquet Discount', 700);";
        cmd.ExecuteNonQuery();
    }
}