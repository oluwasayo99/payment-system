using Demo.Common.Postgres;
using Demo.Ledger.Service.Services;
using FluentAssertions;
using DbUp;
using Dapper;
using Microsoft.Extensions.Options;
using Demo.Common.Settings;

namespace Demo.Ledger.Tests.Services;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<Integration.DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[Collection("Database collection")]
public class LedgerServiceTests
{
    private readonly Integration.DatabaseFixture _fixture;
    private readonly LedgerService _sut;
    private readonly DbConnectionFactory _dbConnectionFactory;

    public LedgerServiceTests(Integration.DatabaseFixture fixture)
    {
        _fixture = fixture;

        // Run migrations
        EnsureDatabase.For.PostgresqlDatabase(_fixture.ConnectionString);
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_fixture.ConnectionString)
            .WithScriptsEmbeddedInAssembly(typeof(LedgerService).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        result.Successful.Should().BeTrue();

        var databaseSettings = Options.Create(new DatabaseSettings
        {
            ConnectionString = _fixture.ConnectionString
        });

        // Manual DI essentially
        _dbConnectionFactory = new DbConnectionFactory(databaseSettings);
        _sut = new LedgerService(_dbConnectionFactory);
    }

    [Fact]
    public async Task ReserveAsync_ShouldReserveAmount_WhenWalletHasSufficientFunds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 500m, 0m);

        // Act
        var result = await _sut.ReserveAsync(userId, 100m);

        // Assert
        result.Should().NotBeNull();

        var wallet = await GetWalletAsync(userId);
        ((decimal)wallet.balance).Should().Be(500m);
        ((decimal)wallet.held_balance).Should().Be(100m);
    }

    [Fact]
    public async Task ReserveAsync_ShouldThrowException_WhenWalletHasInsufficientFunds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await SeedWalletAsync(userId, 50m, 0m);

        // Act & Assert
        await FluentActions.Invoking(() => _sut.ReserveAsync(userId, 100m))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*Insufficient funds available*");
    }

    private async Task SeedWalletAsync(Guid userId, decimal balance, decimal heldBalance)
    {
        await using var conn = await _dbConnectionFactory.CreateConnectionAsync();
        await conn.ExecuteAsync(
            "INSERT INTO wallets (user_id, balance, held_balance) VALUES (@userId, @balance, @heldBalance)",
            new { userId, balance, heldBalance }
        );
    }

    private async Task<dynamic> GetWalletAsync(Guid userId)
    {
        await using var conn = await _dbConnectionFactory.CreateConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT balance, held_balance FROM wallets WHERE user_id = @userId",
            new { userId }
        );
    }
}
