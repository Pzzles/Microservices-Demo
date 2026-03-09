using System.ComponentModel.DataAnnotations;

namespace ProductService.Models.DTOs
{
    public class CreateProductRequestDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
    }
}
