using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DistributedCaching.Extensions
{
    public static class DistributedCacheExtensions
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = null,
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// By default, any cache key would have an absolute expiration of 1 hour, and sliding expiration period of 30 minutes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Task SetAsync<T>(this IDistributedCache cache, string key, T value)
        {
            return cache.SetAsync(key, value, new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1)));
        }

        /// <summary>
        /// This methods helps you serialize the incoming value of type T, and then forming the byte array. This data, along with the cache key is passed on to the original SetAsync method. We are also passing JsonSerializerOptions instance while serializing the data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, serializerOptions));

            return cache.SetAsync(key, bytes, options);
        }

        /// <summary>
        /// This method fetches the data from cache based on the passed cache key. If found, it deserializes the data into type T and sets the value, and passes back a true. If the key is not found in the Redis cache memory, it returns false.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetValue<T>(this IDistributedCache cache, string key, out T? value)
        {
            byte[]? val = cache.Get(key);

            value = default;

            if (val == null)
            {
                return false;
            }

            value = JsonSerializer.Deserialize<T>(val, serializerOptions);

            return true;
        }

        /// <summary>
        /// This is a wrapper around both of the above extensions. Basically, in this single method, it handles both the Get and Set operations flawlessly. If the Cache Key is found in Redis, it returns the data. And if it’s not found, it executes the passed task (lambda function), and sets the returned value to the cache memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="task"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static async Task<T?> GetOrSetAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> task, DistributedCacheEntryOptions? options = null)
        {
            options ??= new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            if (cache.TryGetValue(key, out T? value) && value is not null)
            {
                return value;
            }

            value = await task();

            if (value is not null)
            {
                await cache.SetAsync(key, value, options);
            }

            return value;
        }
    }
}