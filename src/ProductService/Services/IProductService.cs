using ProductService.Models.DTOs;

namespace ProductService.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllAsync(string? category);
        Task<ProductResponseDto?> GetByIdAsync(Guid id);
        Task<ProductResponseDto> CreateAsync(CreateProductRequestDto dto);
        Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductRequestDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
