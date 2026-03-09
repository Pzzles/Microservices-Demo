using OrderService.Models.DTOs;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task<(OrderResponseDto? order, string? error)> CreateAsync(CreateOrderRequestDto dto);
        Task<OrderResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderResponseDto>> GetByUserIdAsync(Guid userId);
        Task<OrderResponseDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto);
    }
}
