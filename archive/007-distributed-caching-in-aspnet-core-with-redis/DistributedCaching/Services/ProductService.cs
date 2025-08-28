using DistributedCaching.Domain;
using DistributedCaching.Extensions;
using DistributedCaching.Models;
using DistributedCaching.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace DistributedCaching.Services;

public class ProductService(AppDbContext context, IDistributedCache cache, ILogger<ProductService> logger) : IProductService
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
        await InvalidateProductCache(id);

        // invalidate cache for products, as existing product is deleted
        await InvalidateProductsCache();

        logger.LogInformation("Product with id {ProductId} deleted successfully.", id);

        return true;
    }

    /*

    public async Task<ProductDto?> GetProduct(Guid id)
    {
        string cacheKey = $"product:{id}";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", cacheKey);

        byte[]? cached = await cache.GetAsync(cacheKey);

        if (cached is not null)
        {
            logger.LogInformation("cache hit for key: {CacheKey}.", cacheKey);

            return JsonSerializer.Deserialize<ProductDto>(cached);
        }

        logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", cacheKey);

        Product? entity = await context.Products.FindAsync(id);

        if (entity is null)
        {
            return null;
        }

        ProductDto product = new(entity.Id, entity.Name, entity.Description, entity.Price);

        await cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(product),
            CreateCacheOptions(50, 5));

        logger.LogInformation("setting data for key: {CacheKey} to cache.", cacheKey);

        return product;
    }

    */

    public async Task<ProductDto?> GetProduct(Guid id)
    {
        string cacheKey = $"product:{id}";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", cacheKey);

        return await cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", cacheKey);

                Product? entity = await context.Products.FindAsync(id);

                return entity is null
                    ? null
                    : new ProductDto(entity.Id, entity.Name, entity.Description, entity.Price);
            },
            CreateCacheOptions(50, 5)); // Time-Based Invalidation
    }

    /*

    public async Task<List<ProductDto>> GetProducts()
    {
        string cacheKey = "products";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", cacheKey);

        byte[]? cached = await cache.GetAsync(cacheKey);

        if (cached is not null)
        {
            logger.LogInformation("cache hit for key: {CacheKey}.", cacheKey);

            return JsonSerializer.Deserialize<List<ProductDto>>(cached) ?? [];
        }

        logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", cacheKey);

        List<ProductDto> products = await context.Products
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
            .ToListAsync();

        await cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(products),
            CreateCacheOptions(20, 2));

        logger.LogInformation("setting data for key: {CacheKey} to cache.", cacheKey);

        return products ?? [];
    }

    */

    public async Task<List<ProductDto>> GetProducts()
    {
        string cacheKey = "products";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", cacheKey);

        return await cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", cacheKey);

                return await context.Products
                    .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price))
                    .ToListAsync();
            },
            CreateCacheOptions(20, 2)) ?? [];
    }

    public async Task<ProductDto?> AddProduct(ProductCreateDto request)
    {
        Product product = new(request.Name, request.Description, request.Price);

        await context.Products.AddAsync(product);

        await context.SaveChangesAsync();

        // invalidate cache for products, as new product is added
        await InvalidateProductsCache();

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
        await InvalidateProductCache(id);

        // invalidate cache for products, as existing product is updated
        await InvalidateProductsCache();

        logger.LogInformation("Product with id {ProductId} updated successfully.", id);

        return new ProductDto(product.Id, product.Name, product.Description, product.Price);
    }

    private async Task InvalidateProductCache(Guid id)
    {
        string cacheKey = $"product:{id}";

        await cache.RemoveAsync(cacheKey); // Manual Invalidation

        logger.LogInformation("invalidating cache for key: {CacheKey} from cache.", cacheKey);
    }

    private async Task InvalidateProductsCache()
    {
        string cacheKey = "products";

        await cache.RemoveAsync(cacheKey); // Manual Invalidation

        logger.LogInformation("invalidating cache for key: {CacheKey} from cache.", cacheKey);
    }

    private static DistributedCacheEntryOptions CreateCacheOptions(
       int absoluteExpirationMinutes, int slidingExpirationMinutes)
    {
        // SetAbsoluteExpiration: Defines a fixed time after which the cache entry will expire, regardless of how often it is accessed. This prevents the cache from serving outdated data indefinitely.
        // SetSlidingExpiration: Sets a time interval during which the cache entry will expire if not accessed.
        // Best Practice:Always use both sliding and absolute expiration together. Ensure that the absolute expiration time is longer than the sliding expiration time to avoid conflicts.
        return new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(absoluteExpirationMinutes)) // Time-Based Invalidation (TimeSpan | DateTimeOffset)
            .SetSlidingExpiration(TimeSpan.FromMinutes(slidingExpirationMinutes)); // Time-Based Invalidation (TimeSpan)
    }
}