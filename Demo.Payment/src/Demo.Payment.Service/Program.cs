using Demo.Common.Ledger;
using Demo.Common.MassTransit;
using Demo.Common.Mongo;
using Demo.Common.Redis;
using Demo.Payments.Service.Entities;
using Demo.Payments.Service.Filters;
using Demo.Payments.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddMongo()
    .AddMongoRepository<PaymentIntent>()
    .AddMassTransitWithRabbitMq()
    .AddRedis()
    .AddLedgerClient(builder.Configuration);


builder.Services.AddScoped<IdempotencyFilter>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    // app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();