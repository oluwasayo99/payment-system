using Demo.Common.Redis;
using StackExchange.Redis;

namespace Demo.Common.Settings;

public class RedisService : IRedisService
{
    public readonly IDatabase database;

    public RedisService(IConnectionMultiplexer multiplexer)
    {
        database = multiplexer.GetDatabase();
    }

    public async Task<bool> TryAcquireLockAsync(string key, TimeSpan expiry)
    {
        return await database.StringSetAsync(key, "LOCK", expiry, When.NotExists);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await database.StringGetAsync(key);
    }

    public async Task RemoveAsync(string key)
    {
       await database.KeyDeleteAsync(key);
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry)
    {
        await database.StringSetAsync(key, value, expiry);
    }
}