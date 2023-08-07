using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace WebApi.Extensions;

public static class DistributedCacheExtensions
{
    private const string CacheKey = "PessoaCache";

    public static async Task<T> GetCacheAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> fetchDataFunc, TimeSpan? expirationTime = null)
    {
        var cachedData = await cache.GetStringAsync($"{CacheKey}:{key}");
        if (cachedData is not null)
        {
            return JsonSerializer.Deserialize<T>(cachedData)!;
        }

        var data = await fetchDataFunc();
        if (data is not null)
        {
            var serializedData = JsonSerializer.Serialize(data);
            await cache.SetStringAsync(key, serializedData, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromSeconds(30)
            });
        }

        return data;
    }
}