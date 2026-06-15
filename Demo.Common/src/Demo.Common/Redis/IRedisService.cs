namespace Demo.Common.Redis;


public interface IRedisService
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value, TimeSpan expiry);
    Task<bool> TryAcquireLockAsync(string key, TimeSpan expiry);

    Task RemoveAsync(string key);
}