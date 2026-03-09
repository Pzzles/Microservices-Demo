using ProductService.Models.DTOs;
using ProductService.Repositories;
using ProductService.Utils;

namespace ProductService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllAsync(string? category)
        {
            var products = await _repository.GetAllAsync(category);
            return products.Select(ProductMapper.ToResponseDto);
        }

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var product = await _repository.GetByIdAsync(id);
            return product is null ? null : ProductMapper.ToResponseDto(product);
        }

        public async Task<ProductResponseDto> CreateAsync(CreateProductRequestDto dto)
        {
            var product = ProductMapper.ToEntity(dto);
            await _repository.AddAsync(product);
            return ProductMapper.ToResponseDto(product);
        }

        public async Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
            {
                return null;
            }

            ProductMapper.ApplyUpdate(dto, existing);
            await _repository.UpdateAsync(existing);
            return ProductMapper.ToResponseDto(existing);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            return _repository.DeleteAsync(id);
        }
    }
}
