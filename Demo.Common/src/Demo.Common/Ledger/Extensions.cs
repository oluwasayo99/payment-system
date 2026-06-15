using Demo.Common.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace Demo.Common.Ledger;

public static class Extensions
{
    public static IServiceCollection AddLedgerClient(this IServiceCollection services, IConfiguration configuration)
    {

        services.Configure<LedgerClientSettings>(
            configuration.GetSection(nameof(LedgerClientSettings))
        );

        Random jitterer = new Random();

        services.AddHttpClient<LedgerClient>((sp, client) =>
        {
            var settings = sp
                .GetRequiredService<IOptions<LedgerClientSettings>>()
                .Value;
            if (string.IsNullOrEmpty(settings.BaseUrl))
            {
                throw new InvalidOperationException(
                    "LedgerClientSettings.BaseUrl is not configured"
                );
            }
            client.BaseAddress = new Uri(settings.BaseUrl);
        })
        .AddTransientHttpErrorPolicy(policy => policy.Or<TimeoutRejectedException>().WaitAndRetryAsync(
            5,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
            onRetry: (outcome, timespan, retryAttempt) =>
            {
                var sp = services.BuildServiceProvider();
                sp.GetService<ILogger<LedgerClient>>()?
                    .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
            }
        ))
        .AddTransientHttpErrorPolicy(policy => policy.Or<TimeoutRejectedException>().CircuitBreakerAsync(
            3,
            TimeSpan.FromSeconds(15),
            onBreak: (outcome, timespan) =>
            {
                var sp = services.BuildServiceProvider();
                sp.GetService<ILogger<LedgerClient>>()?
                    .LogWarning($"Opening circuit for timespan {timespan.TotalSeconds} seconds ...");
            },
            onReset: () =>
            {
                var sp = services.BuildServiceProvider();
                sp.GetService<ILogger<LedgerClient>>()?
                    .LogWarning($"Closing the circuit");
            }
        ))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));

        return services;
    }
}