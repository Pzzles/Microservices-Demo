using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProductService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "ImageUrl", "Name", "Price", "StockQuantity" },
                values: new object[,]
                {
                    { new Guid("1a1608a0-0ca6-4ef8-bbe6-a729e6087810"), "Apparel", new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Comfort-fit t-shirt with breathable fabric.", "https://example.com/images/devops-tee.jpg", "DevOps Tee", 29.95m, 140 },
                    { new Guid("27481f09-16f1-4690-8583-c9f89082a004"), "Laptops", new DateTime(2026, 1, 4, 0, 0, 0, 0, DateTimeKind.Utc), "14-inch performance laptop for creators and developers.", "https://example.com/images/ultrabook-z5.jpg", "UltraBook Z5", 1499.00m, 22 },
                    { new Guid("2ab5bf65-198c-4767-b6f3-89c0c6ace507"), "Accessories", new DateTime(2026, 1, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Noise-cancelling wireless earbuds with transparency mode.", "https://example.com/images/pulse-buds-pro.jpg", "Pulse Buds Pro", 199.99m, 120 },
                    { new Guid("2f06c741-8a2f-47f1-8c74-bf0832f1c101"), "Phones", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Flagship Android phone with 6.7-inch OLED display.", "https://example.com/images/pixel-nova-x.jpg", "Pixel Nova X", 899.99m, 45 },
                    { new Guid("56ea5d5e-6d9f-42ee-9d87-0619cf3e1b02"), "Phones", new DateTime(2026, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Premium smartphone with advanced camera stabilization.", "https://example.com/images/ifruit-pro-14.jpg", "iFruit Pro 14", 1099.00m, 30 },
                    { new Guid("64f37d4e-b3ab-4878-85a2-05d7c5aca908"), "Accessories", new DateTime(2026, 1, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Fast USB-C charger compatible with phones and laptops.", "https://example.com/images/voltcharge-65w.jpg", "VoltCharge 65W", 49.99m, 200 },
                    { new Guid("79bcbf88-c1e0-4a0f-9ee4-669ac986f505"), "Laptops", new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Business laptop with robust security and docking support.", "https://example.com/images/workmate-15.jpg", "WorkMate 15", 1199.99m, 18 },
                    { new Guid("977ae08c-a68f-48c7-8ccf-b43db4b93809"), "Apparel", new DateTime(2026, 1, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Soft cotton-blend hoodie with minimalist design.", "https://example.com/images/cloudrunner-hoodie.jpg", "CloudRunner Hoodie", 69.00m, 85 },
                    { new Guid("de7cbf8d-b151-402e-a13e-3cedf6a60706"), "Laptops", new DateTime(2026, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Gaming laptop with dedicated GPU and 240Hz display.", "https://example.com/images/gamecore-g17.jpg", "GameCore G17", 1899.95m, 12 },
                    { new Guid("ebf67c77-6991-4f1d-a8e6-353f18d8fe03"), "Phones", new DateTime(2026, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Lightweight smartphone with long battery life.", "https://example.com/images/galaxy-aero-lite.jpg", "Galaxy Aero Lite", 649.50m, 60 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
