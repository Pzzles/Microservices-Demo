using Microsoft.EntityFrameworkCore;
using ProductService.Models.Entities;

namespace ProductService.Data
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var product = modelBuilder.Entity<Product>();
            product.HasKey(p => p.Id);
            product.Property(p => p.Name).IsRequired().HasMaxLength(200);
            product.Property(p => p.Description).IsRequired().HasMaxLength(2000);
            product.Property(p => p.Category).IsRequired().HasMaxLength(100);
            product.Property(p => p.Price).HasColumnType("decimal(18,2)");
            product.Property(p => p.CreatedAt).IsRequired();
            product.HasIndex(p => p.Category);

            product.HasData(
                new Product
                {
                    Id = Guid.Parse("2f06c741-8a2f-47f1-8c74-bf0832f1c101"),
                    Name = "Pixel Nova X",
                    Description = "Flagship Android phone with 6.7-inch OLED display.",
                    Price = 899.99m,
                    Category = "Phones",
                    ImageUrl = "https://example.com/images/pixel-nova-x.jpg",
                    StockQuantity = 45,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("56ea5d5e-6d9f-42ee-9d87-0619cf3e1b02"),
                    Name = "iFruit Pro 14",
                    Description = "Premium smartphone with advanced camera stabilization.",
                    Price = 1099.00m,
                    Category = "Phones",
                    ImageUrl = "https://example.com/images/ifruit-pro-14.jpg",
                    StockQuantity = 30,
                    CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("ebf67c77-6991-4f1d-a8e6-353f18d8fe03"),
                    Name = "Galaxy Aero Lite",
                    Description = "Lightweight smartphone with long battery life.",
                    Price = 649.50m,
                    Category = "Phones",
                    ImageUrl = "https://example.com/images/galaxy-aero-lite.jpg",
                    StockQuantity = 60,
                    CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("27481f09-16f1-4690-8583-c9f89082a004"),
                    Name = "UltraBook Z5",
                    Description = "14-inch performance laptop for creators and developers.",
                    Price = 1499.00m,
                    Category = "Laptops",
                    ImageUrl = "https://example.com/images/ultrabook-z5.jpg",
                    StockQuantity = 22,
                    CreatedAt = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("79bcbf88-c1e0-4a0f-9ee4-669ac986f505"),
                    Name = "WorkMate 15",
                    Description = "Business laptop with robust security and docking support.",
                    Price = 1199.99m,
                    Category = "Laptops",
                    ImageUrl = "https://example.com/images/workmate-15.jpg",
                    StockQuantity = 18,
                    CreatedAt = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("de7cbf8d-b151-402e-a13e-3cedf6a60706"),
                    Name = "GameCore G17",
                    Description = "Gaming laptop with dedicated GPU and 240Hz display.",
                    Price = 1899.95m,
                    Category = "Laptops",
                    ImageUrl = "https://example.com/images/gamecore-g17.jpg",
                    StockQuantity = 12,
                    CreatedAt = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("2ab5bf65-198c-4767-b6f3-89c0c6ace507"),
                    Name = "Pulse Buds Pro",
                    Description = "Noise-cancelling wireless earbuds with transparency mode.",
                    Price = 199.99m,
                    Category = "Accessories",
                    ImageUrl = "https://example.com/images/pulse-buds-pro.jpg",
                    StockQuantity = 120,
                    CreatedAt = new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("64f37d4e-b3ab-4878-85a2-05d7c5aca908"),
                    Name = "VoltCharge 65W",
                    Description = "Fast USB-C charger compatible with phones and laptops.",
                    Price = 49.99m,
                    Category = "Accessories",
                    ImageUrl = "https://example.com/images/voltcharge-65w.jpg",
                    StockQuantity = 200,
                    CreatedAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("977ae08c-a68f-48c7-8ccf-b43db4b93809"),
                    Name = "CloudRunner Hoodie",
                    Description = "Soft cotton-blend hoodie with minimalist design.",
                    Price = 69.00m,
                    Category = "Apparel",
                    ImageUrl = "https://example.com/images/cloudrunner-hoodie.jpg",
                    StockQuantity = 85,
                    CreatedAt = new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("1a1608a0-0ca6-4ef8-bbe6-a729e6087810"),
                    Name = "DevOps Tee",
                    Description = "Comfort-fit t-shirt with breathable fabric.",
                    Price = 29.95m,
                    Category = "Apparel",
                    ImageUrl = "https://example.com/images/devops-tee.jpg",
                    StockQuantity = 140,
                    CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
