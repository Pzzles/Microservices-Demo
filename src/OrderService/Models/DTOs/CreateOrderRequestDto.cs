using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.DTOs
{
    public class CreateOrderRequestDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
