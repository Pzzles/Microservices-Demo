namespace OrderService.Services
{
    public interface IProductValidationService
    {
        Task<ProductDto?> GetProductAsync(Guid productId);
    }
}
