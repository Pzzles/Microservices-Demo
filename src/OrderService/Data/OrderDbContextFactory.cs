using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OrderService.Data
{
    public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
    {
        public OrderDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                var projectPath = Path.Combine(basePath, "src", "OrderService");
                if (File.Exists(Path.Combine(projectPath, "appsettings.json")))
                {
                    basePath = projectPath;
                }
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration["ConnectionStrings__OrderDb"]
                ?? "Host=localhost;Port=5432;Database=order_service_db;Username=postgres;Password=YOUR_PASSWORD_HERE";

            var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new OrderDbContext(optionsBuilder.Options);
        }
    }
}
