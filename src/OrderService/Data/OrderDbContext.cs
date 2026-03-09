using Microsoft.EntityFrameworkCore;
using OrderService.Models.Entities;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var order = modelBuilder.Entity<Order>();
            order.HasKey(o => o.Id);
            order.Property(o => o.ProductName).IsRequired().HasMaxLength(200);
            order.Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");
            order.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            order.Property(o => o.Status).HasConversion<string>();
            order.Property(o => o.CreatedAt).IsRequired();
            order.HasIndex(o => o.UserId);
        }
    }
}
