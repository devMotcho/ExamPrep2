using Auth.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.Infrastructure.Security;

public class RedisJwtBlocklistService(IDistributedCache cache) : IJwtBlocklistService
{
    private const string BlocklistPrefix = "jwt:blocklist:";

    public async Task BlockTokenAsync(string jti, TimeSpan timeToLive)
    {
        var cacheKey = $"{BlocklistPrefix}{jti}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = timeToLive
        };

        // We only care if the key exists, the value doesn't matter
        await cache.SetStringAsync(cacheKey, "revoked", options);
    }

    public async Task<bool> IsTokenBlockedAsync(string jti)
    {
        var cacheKey = $"{BlocklistPrefix}{jti}";
        var value = await cache.GetStringAsync(cacheKey);
        
        return !string.IsNullOrEmpty(value);
    }
}
