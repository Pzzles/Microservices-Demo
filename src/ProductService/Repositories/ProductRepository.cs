using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models.Entities;

namespace ProductService.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _dbContext;

        public ProductRepository(ProductDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(string? category)
        {
            var query = _dbContext.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public Task<Product?> GetByIdAsync(Guid id)
        {
            return _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            await _dbContext.Products.AddAsync(product);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (existing is null)
            {
                return false;
            }

            _dbContext.Products.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
