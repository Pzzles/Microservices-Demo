using System.Text.Json;

namespace OrderService.Services
{
    public class ProductValidationService : IProductValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // ─────────────────────────────────────────────────────────────────
        // TEMPORARY: ProductService is called directly via HTTP for local
        // development. When WSO2 API Manager is configured, the base URL
        // (ServiceUrls__ProductService in config) must be updated to point
        // to the WSO2 gateway endpoint instead of the direct service URL.
        // The code requires no changes — only the configuration value changes.
        // See Phase 3: WSO2 Configuration in the project documentation.
        // ─────────────────────────────────────────────────────────────────
        public ProductValidationService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ProductDto?> GetProductAsync(Guid productId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ProductService");
                using var response = await client.GetAsync($"/api/products/{productId}");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductDto>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
