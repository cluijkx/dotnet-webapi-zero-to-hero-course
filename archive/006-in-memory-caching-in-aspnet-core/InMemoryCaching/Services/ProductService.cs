using InMemoryCaching.Models;
using InMemoryCaching.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InMemoryCaching.Services;

public class ProductService(AppDbContext context, IMemoryCache cache, ILogger<ProductService> logger) : IProductService
{
    public async Task<Product?> Get(Guid id)
    {
        string cacheKey = $"product:{id}";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", cacheKey);

        if (!cache.TryGetValue(cacheKey, out Product? product))
        {
            logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", cacheKey);

            product = await context.Products.FindAsync(id);

            // SetPriority: Sets the priority of retaining the cache entry. This determines the likelihood of the entry being removed when the cache exceeds memory limits.
            // SetSize: Specifies the size of the cache entry. This helps prevent the cache from consuming excessive server resources.
            // SetAbsoluteExpiration: Defines a fixed time after which the cache entry will expire, regardless of how often it is accessed. This prevents the cache from serving outdated data indefinitely.
            // SetSlidingExpiration: Sets a time interval during which the cache entry will expire if not accessed.
            // Best Practice:Always use both sliding and absolute expiration together. Ensure that the absolute expiration time is longer than the sliding expiration time to avoid conflicts.
            MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions() // Time-Based Invalidation
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(50))
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetPriority(CacheItemPriority.Normal);

            logger.LogInformation("setting data for key: {CacheKey} to cache.", cacheKey);

            cache.Set(cacheKey, product, cacheOptions);
        }
        else
        {
            logger.LogInformation("cache hit for key: {CacheKey}.", cacheKey);
        }

        return product;
    }

    public async Task<List<Product>> GetAll()
    {
        string cacheKey = "products";

        logger.LogInformation("fetching data for key: {CacheKey} from cache.", cacheKey);

        if (!cache.TryGetValue(cacheKey, out List<Product>? products))
        {
            logger.LogInformation("cache miss. fetching data for key: {CacheKey} from database.", cacheKey);

            products = await context.Products.ToListAsync();

            // SetPriority: Sets the priority of retaining the cache entry. This determines the likelihood of the entry being removed when the cache exceeds memory limits.
            // SetSize: Specifies the size of the cache entry. This helps prevent the cache from consuming excessive server resources.
            // SetAbsoluteExpiration: Defines a fixed time after which the cache entry will expire, regardless of how often it is accessed. This prevents the cache from serving outdated data indefinitely.
            // SetSlidingExpiration: Sets a time interval during which the cache entry will expire if not accessed.
            // Best Practice:Always use both sliding and absolute expiration together. Ensure that the absolute expiration time is longer than the sliding expiration time to avoid conflicts.
            MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions() // Time-Based Invalidation
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(20))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2))
                .SetPriority(CacheItemPriority.NeverRemove)
                .SetSize(2048);

            logger.LogInformation("setting data for key: {CacheKey} to cache.", cacheKey);

            cache.Set(cacheKey, products, cacheOptions);
        }
        else
        {
            logger.LogInformation("cache hit for key: {CacheKey}.", cacheKey);
        }

        return products ?? [];
    }

    public async Task Add(ProductCreationDto request)
    {
        Product product = new(request.Name, request.Description, request.Price);

        await context.Products.AddAsync(product);

        await context.SaveChangesAsync();

        // invalidate cache for products, as new product is added
        string cacheKey = "products";

        logger.LogInformation("invalidating cache for key: {CacheKey} from cache.", cacheKey);

        cache.Remove(cacheKey); // Manual Invalidation
    }
}