namespace Demo.Common.Mongo;
using Demo.Common.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

public static class Extensions
{
    public static IServiceCollection AddMongo(this IServiceCollection services)
    {
        MongoMappings.RegisterMappings();
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var serviceSettings = configuration.GetRequiredSection(nameof(ServiceSettings)).Get<ServiceSettings>();
            var mongoSettings = configuration.GetRequiredSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
            Console.WriteLine($"Connection String Settings: {mongoSettings.ConnectionString}");
            var client = new MongoClient(mongoSettings.ConnectionString);
            return client.GetDatabase(serviceSettings.ServiceName);
        });
        return services;
    }

    public static IServiceCollection AddMongoRepository<T>(this IServiceCollection services) where T : IEntity
    {
        services.AddSingleton<IRepository<T>>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<T>(database);
        });
        return services;
    }
}