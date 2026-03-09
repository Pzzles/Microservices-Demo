using System.ComponentModel.DataAnnotations;
using OrderService.Models.Enums;

namespace OrderService.Models.DTOs
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus Status { get; set; }
    }
}
