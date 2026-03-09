using System.ComponentModel.DataAnnotations;

namespace ProductService.Models.DTOs
{
    public class UpdateProductRequestDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        public string? Category { get; set; }
        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }
    }
}
