using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace PrintGrid.Infrastructure.Shared.Caching;

public abstract class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await cache.GetStringAsync(key, cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<T>(payload, _serializerOptions);
    }
}
