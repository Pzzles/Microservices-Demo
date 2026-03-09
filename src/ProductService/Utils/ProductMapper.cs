using ProductService.Models.DTOs;
using ProductService.Models.Entities;

namespace ProductService.Utils
{
    public static class ProductMapper
    {
        public static Product ToEntity(CreateProductRequestDto dto)
        {
            return new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                Price = dto.Price,
                Category = dto.Category.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
                StockQuantity = dto.StockQuantity,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(UpdateProductRequestDto dto, Product entity)
        {
            if (dto.Name is not null)
            {
                entity.Name = dto.Name.Trim();
            }

            if (dto.Description is not null)
            {
                entity.Description = dto.Description.Trim();
            }

            if (dto.Price.HasValue)
            {
                entity.Price = dto.Price.Value;
            }

            if (dto.Category is not null)
            {
                entity.Category = dto.Category.Trim();
            }

            if (dto.ImageUrl is not null)
            {
                entity.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
            }

            if (dto.StockQuantity.HasValue)
            {
                entity.StockQuantity = dto.StockQuantity.Value;
            }
        }

        public static ProductResponseDto ToResponseDto(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity,
                CreatedAt = product.CreatedAt
            };
        }
    }
}
