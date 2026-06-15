namespace Demo.Common.Postgres;

using Demo.Common.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


public static class Extensions
{
    public static IServiceCollection AddPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseSettings>(options =>
        {
            configuration
                .GetSection(nameof(DatabaseSettings))
                .Bind(options);
        });

        services.AddSingleton<DbConnectionFactory>();

        return services;
    }
}