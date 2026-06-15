using Demo.Common.Settings;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Demo.Common.Postgres;

public class DbConnectionFactory
{
    private readonly string connectionString;

    public DbConnectionFactory(IOptions<DatabaseSettings> settings)
    {
        this.connectionString = settings.Value.ConnectionString;
    }

    public async Task<NpgsqlConnection> CreateConnectionAsync()
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}