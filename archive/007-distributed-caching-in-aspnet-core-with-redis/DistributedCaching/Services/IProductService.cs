using DistributedCaching.Models;

namespace DistributedCaching.Services;

public interface IProductService
{
    Task<bool> DeleteProduct(Guid id);

    Task<List<ProductDto>> GetProducts();

    Task<ProductDto?> GetProduct(Guid id);

    Task<ProductDto?> AddProduct(ProductCreateDto request);

    Task<ProductDto?> UpdateProduct(Guid id, ProductUpdateDto request);
}