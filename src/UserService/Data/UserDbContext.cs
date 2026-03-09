using Microsoft.EntityFrameworkCore;
using UserService.Models.Entities;

namespace UserService.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var user = modelBuilder.Entity<User>();

            user.HasKey(u => u.Id);

            user.Property(u => u.CognitoSub)
                .IsRequired()
                .HasMaxLength(128);

            user.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(200);

            user.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(320);

            user.Property(u => u.CreatedAt)
                .IsRequired();

            user.HasIndex(u => u.Email).IsUnique();
            user.HasIndex(u => u.CognitoSub).IsUnique();
        }
    }
}
