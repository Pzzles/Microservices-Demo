using OrderService.Models.DTOs;
using OrderService.Repositories;
using OrderService.Utils;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IProductValidationService _productValidationService;

        public OrderService(IOrderRepository repository, IProductValidationService productValidationService)
        {
            _repository = repository;
            _productValidationService = productValidationService;
        }

        public async Task<(OrderResponseDto? order, string? error)> CreateAsync(CreateOrderRequestDto dto)
        {
            var product = await _productValidationService.GetProductAsync(dto.ProductId);
            if (product is null)
            {
                return (null, "Product not found");
            }

            if (product.StockQuantity < dto.Quantity)
            {
                return (null, "Insufficient stock");
            }

            var order = OrderMapper.ToEntity(dto, product.Name, product.Price);
            await _repository.AddAsync(order);
            return (OrderMapper.ToResponseDto(order), null);
        }

        public async Task<OrderResponseDto?> GetByIdAsync(Guid id)
        {
            var order = await _repository.GetByIdAsync(id);
            return order is null ? null : OrderMapper.ToResponseDto(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetByUserIdAsync(Guid userId)
        {
            var orders = await _repository.GetByUserIdAsync(userId);
            return orders.Select(OrderMapper.ToResponseDto);
        }

        public async Task<OrderResponseDto?> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
            {
                return null;
            }

            existing.Status = dto.Status;
            await _repository.UpdateAsync(existing);
            return OrderMapper.ToResponseDto(existing);
        }
    }
}
