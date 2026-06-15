
using Demo.Common.Ledger;
using Demo.Common.MassTransit;
using Demo.Settlement.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMassTransitWithRabbitMq()
    .AddLedgerClient(builder.Configuration);

builder.Services.AddScoped<BankService>();

var host = builder.Build();
host.Run();
