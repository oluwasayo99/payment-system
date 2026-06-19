
using Dapper;
using Demo.Common.Postgres;

namespace Demo.Ledger.Service.Services;

public class LedgerService
{
    private readonly DbConnectionFactory dbConnectionFactory;

    public LedgerService(DbConnectionFactory dbConnectionFactory)
    {
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<object> ReserveAsync(Guid userId, decimal amount)
    {
        await using var conn = await dbConnectionFactory.CreateConnectionAsync();

        try
        {
            var reservationId = await conn.ExecuteScalarAsync<Guid>(
                "CALL process_reservation(@userId, @amount, null)", 
                new { userId, amount }
            );

            return new { status = "RESERVED", reservationId };
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task<object> ReleaseAsync(Guid reservationId)
    {
        await using var conn = await dbConnectionFactory.CreateConnectionAsync();

        try
        {
            await conn.ExecuteAsync("CALL release_reservation(@reservationId)", new { reservationId });
            return new { status = "RELEASED", reservationId };
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task<object> SettleAsync(Guid reservationId)
    {
        await using var conn = await dbConnectionFactory.CreateConnectionAsync();

        try
        {
            await conn.ExecuteAsync("CALL settle_reservation(@reservationId)", new { reservationId });
            return new { status = "SETTLED" };
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}