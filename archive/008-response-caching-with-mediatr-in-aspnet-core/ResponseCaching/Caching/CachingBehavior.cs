using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace ResponseCaching.Caching;

public class CachingBehavior<TRequest, TResponse>(ILogger<CachingBehavior<TRequest, TResponse>> logger, IDistributedCache cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheable
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        TResponse response;

        if (request.BypassCache)
        {
            return await next();// skip entire pipeline
        }

        async Task<TResponse> GetResponseAndAddToCache()
        {
            response = await next();

            if (response != null)
            {
                int slidingExpiration = request.SlidingExpirationInMinutes == 0 ? 30 : request.SlidingExpirationInMinutes;
                int absoluteExpiration = request.AbsoluteExpirationInMinutes == 0 ? 60 : request.AbsoluteExpirationInMinutes;
                
                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(slidingExpiration))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(absoluteExpiration));

                byte[] serializedData = Encoding.Default.GetBytes(JsonSerializer.Serialize(response));

                await cache.SetAsync(request.CacheKey, serializedData, options, cancellationToken);
            }

            return response;
        }

        byte[]? cachedResponse = await cache.GetAsync(request.CacheKey, cancellationToken);

        if (cachedResponse != null)
        {
            response = JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cachedResponse))!;

            logger.LogInformation("fetched from cache with key : {CacheKey}", request.CacheKey);

            cache.Refresh(request.CacheKey);
        }
        else
        {
            response = await GetResponseAndAddToCache();

            logger.LogInformation("added to cache with key : {CacheKey}", request.CacheKey);
        }

        return response;
    }
}