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

            LoadEnvironmentFromDotEnv(basePath);

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

        private static void LoadEnvironmentFromDotEnv(string basePath)
        {
            var dotEnvPath = Path.Combine(basePath, ".env");
            if (!File.Exists(dotEnvPath))
            {
                return;
            }

            foreach (var rawLine in File.ReadAllLines(dotEnvPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                {
                    line = line[7..].Trim();
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
}
