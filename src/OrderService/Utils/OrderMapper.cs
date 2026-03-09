using OrderService.Models.DTOs;
using OrderService.Models.Entities;
using OrderService.Models.Enums;

namespace OrderService.Utils
{
    public static class OrderMapper
    {
        public static Order ToEntity(CreateOrderRequestDto dto, string productName, decimal unitPrice)
        {
            return new Order
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                ProductId = dto.ProductId,
                ProductName = productName,
                Quantity = dto.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * dto.Quantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static OrderResponseDto ToResponseDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                ProductId = order.ProductId,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                UnitPrice = order.UnitPrice,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt
            };
        }
    }
}
