using Demo.Common.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Demo.Common.Redis;

public static class Extensions
{
    public static IServiceCollection AddRedis(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var redisSettings = configuration
                .GetSection(nameof(RedisSettings))
                .Get<RedisSettings>();
            return ConnectionMultiplexer.Connect(redisSettings.ConnectionString);
        });

        services.AddSingleton<IRedisService, RedisService>();
        
        return services;
    }
}