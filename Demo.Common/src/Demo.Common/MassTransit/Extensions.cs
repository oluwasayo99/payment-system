using System.Reflection;
using Demo.Common.Settings;
using GreenPipes;
using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Common.MassTransit;

public static class Extensions
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services)
    {
        services.AddMassTransit(config =>
        {
            config.AddConsumers(Assembly.GetEntryAssembly());
            config.UsingRabbitMq((context, configurator) =>
            {
                var configuration = context.GetService<IConfiguration>();
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                var rabbitMqSettings = configuration
                    .GetSection(nameof(RabbitMqSettings))
                    .Get<RabbitMqSettings>();

                configurator.Host(new Uri(rabbitMqSettings.ConnectionUri));
                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter());
                configurator.UseMessageRetry(rc =>
                {
                    rc.Interval(3, TimeSpan.FromSeconds(5));
                });
            });
        });

        services.AddMassTransitHostedService();

        return services;
    }
}

