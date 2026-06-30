using DbUp;
using Demo.Common.Postgres;
using Demo.Common.Redis;
using Demo.Inventory.Service.Cache;
using Demo.Inventory.Service.Services;
using Npgsql;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Product Inventory API",
        Version = "v1",
        Description = "Product Inventory System with query optimization and caching demonstrations"
    });
});

// Register PostgreSQL connection factory
builder.Services.AddSingleton<DbConnectionFactory>(sp =>
{
    var configuration = builder.Configuration;
    var connectionString = configuration.GetSection("DatabaseSettings:ConnectionString").Value;
    return new DbConnectionFactory(
        Microsoft.Extensions.Options.Options.Create(
            new Demo.Common.Settings.DatabaseSettings 
            { 
                ConnectionString = connectionString ?? throw new InvalidOperationException("Connection string not found") 
            }));
});

// Register Redis service
builder.Services.AddRedis();

// Register In-Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IInMemoryCacheService, InMemoryCacheService>();

// Register Inventory Service
builder.Services.AddScoped<InventoryService>();

var app = builder.Build();

// Run database migrations
var connectionString = app.Configuration.GetSection("DatabaseSettings:ConnectionString").Value;
if (!string.IsNullOrEmpty(connectionString))
{
    // Ensure database exists
    EnsureDatabase.For.PostgresqlDatabase(connectionString);
    
    // Run migrations
    var upgrader = DeployChanges.To
        .PostgresqlDatabase(connectionString)
        .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
        .LogToConsole()
        .Build();

    var result = upgrader.PerformUpgrade();
    if (!result.Successful)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Database migration failed: {result.Error}");
        Console.ResetColor();
        // Don't throw - let the app start and fail on actual DB operations
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Database migrations completed successfully!");
        Console.ResetColor();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker" || app.Environment.EnvironmentName == "Kubernetes")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Inventory API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
