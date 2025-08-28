using InMemoryCaching.Domain;
using InMemoryCaching.Models;
using InMemoryCaching.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace InMemoryCaching.Services;

public class ProductService(AppDbContext context, IMemoryCache cache, ILogger<ProductService> logger) : IProductService
{
    public async Task<bool> DeleteProduct(Guid id)
    {
        Product? product = await context.Products.FindAsync(id);

        if (product is null)
        {
            logger.LogWarning("Product with id {ProductId} not found for deletion.", id);

            return false;
        }

        context.Products.Remove(product);

        await context.SaveChangesAsync();

        // invalidate cache for product, as existing product is deleted
        InvalidateProductCache(id);

        // invalidate cache for products, as existing product is deleted
        InvalidateProductsCache();

        logger.LogInformation("Product with id {ProductId} deleted successfully.", id);

        return true;
    }

    public async Task<ProductDto?> GetProduct(Guid id)
    {
        string productCacheKey = $"product:{id}";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", productCacheKey);

        if (cache.TryGetValue(productCacheKey, out ProductDto? product))
        {
            logger.LogInformation("cache hit for key: {CacheKey}.", productCacheKey);

            return product;
        }

        logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", productCacheKey);

        product = (await context.Products.FindAsync(id)) is { } entity
            ? new ProductDto(entity.Id, entity.Name, entity.Description, entity.Price)
            : null;

        if (product is null)
        {
            return null;
        }

        logger.LogInformation("setting data for key: {CacheKey} to cache.", productCacheKey);

        cache.Set(productCacheKey, product,
            CreateCacheOptions(50, 5, CacheItemPriority.Normal));

        return product;
    }

    public async Task<List<ProductDto>> GetProducts()
    {
        string productsCacheKey = "products";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", productsCacheKey);

        if (cache.TryGetValue(productsCacheKey, out List<ProductDto>? products))
        {
            logger.LogInformation("cache hit for key: {CacheKey}.", productsCacheKey);

            return products ?? [];
        }

        logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", productsCacheKey);

        products = await context.Products
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
            .ToListAsync();

        logger.LogInformation("setting data for key: {CacheKey} to cache.", productsCacheKey);

        cache.Set(productsCacheKey, products,
            CreateCacheOptions(20, 2, CacheItemPriority.NeverRemove, size: 2048));

        return products ?? [];
    }

    public async Task<ProductDto?> AddProduct(ProductCreateDto request)
    {
        Product product = new(request.Name, request.Description, request.Price);

        await context.Products.AddAsync(product);

        await context.SaveChangesAsync();

        // invalidate cache for products, as new product is added
        InvalidateProductsCache();

        logger.LogInformation("Product with id {ProductId} created successfully.", product.Id);

        return new ProductDto(product.Id, product.Name, product.Description, product.Price);
    }

    public async Task<ProductDto?> UpdateProduct(Guid id, ProductUpdateDto request)
    {
        Product? product = await context.Products.FindAsync(id);

        if (product is null)
        {
            return null;
        }

        product.Update(request.Name, request.Description, request.Price);

        await context.SaveChangesAsync();

        // invalidate cache for product, as existing product is updated
        InvalidateProductCache(id);

        // invalidate cache for products, as existing product is updated
        InvalidateProductsCache();

        logger.LogInformation("Product with id {ProductId} updated successfully.", id);

        return new ProductDto(product.Id, product.Name, product.Description, product.Price);
    }

    private void InvalidateProductCache(Guid id)
    {
        string cacheKey = $"product:{id}";

        cache.Remove(cacheKey); // Manual Invalidation

        logger.LogInformation("invalidating cache for key: {CacheKey} from cache.", cacheKey);
    }

    private void InvalidateProductsCache()
    {
        string cacheKey = "products";

        cache.Remove(cacheKey); // Manual Invalidation

        logger.LogInformation("invalidating cache for key: {CacheKey} from cache.", cacheKey);
    }

    private static MemoryCacheEntryOptions CreateCacheOptions(
        int absoluteExpirationMinutes, int slidingExpirationMinutes, CacheItemPriority priority, int? size = null, params IChangeToken[]? changeTokens)
    {
        // SetAbsoluteExpiration: Defines a fixed time after which the cache entry will expire, regardless of how often it is accessed. This prevents the cache from serving outdated data indefinitely.
        // SetSlidingExpiration: Sets a time interval during which the cache entry will expire if not accessed.
        // Best Practice:Always use both sliding and absolute expiration together. Ensure that the absolute expiration time is longer than the sliding expiration time to avoid conflicts.
        // SetPriority: Sets the priority of retaining the cache entry. This determines the likelihood of the entry being removed when the cache exceeds memory limits.
        // SetSize: Specifies the size of the cache entry. This helps prevent the cache from consuming excessive server resources.
        // AddExpirationToken: Links the cache entry lifetime to an external change token (IChangeToken). When the token signals a change (e.g., file update, cancellation token, custom trigger), the cache entry is automatically invalidated.
        MemoryCacheEntryOptions options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(absoluteExpirationMinutes)) // Time-Based Invalidation (TimeSpan | DateTimeOffset)
            .SetSlidingExpiration(TimeSpan.FromMinutes(slidingExpirationMinutes)) // Time-Based Invalidation (TimeSpan)
            .SetPriority(priority); // Capacity-Based invalidation

        if (size.HasValue)
        {
            options.SetSize(size.Value); // Capacity-Based invalidation.
        }

        if (changeTokens is { Length: > 0 })
        {
            foreach (IChangeToken token in changeTokens)
            {
                options.AddExpirationToken(token); // Dependency-Based Invalidation (e.g. IFileProvider.Watch -> IChangeToken)
            }
        }

        return options;
    }
}