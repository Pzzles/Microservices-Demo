using OrderService.Models.Entities;

namespace OrderService.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
    }
}
