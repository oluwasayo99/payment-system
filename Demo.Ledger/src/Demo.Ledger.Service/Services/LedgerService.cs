
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
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var wallet = await conn.QuerySingleOrDefaultAsync<dynamic>(
                @"
                SELECT balance, held_balance
                FROM wallets
                WHERE user_id = @userId
                FOR UPDATE",
                new { userId },
                tx
            );

            if (wallet == null)
            {
                throw new Exception("Wallet not found");
            }

            decimal available = wallet.balance - wallet.held_balance;
            if (available < amount)
            {
                throw new Exception("Insufficient funds available");
            }

            await conn.ExecuteAsync(
                @"
                UPDATE wallets
                SET
                    held_balance = held_balance + @amount
                WHERE user_id = @userId",
                new { userId, amount },
                tx
            );
            var reservationId = await conn.ExecuteScalarAsync<Guid>(
                @"
                INSERT INTO reservations (user_id, amount, status)
                VALUES (@userId, @amount, 'PENDING')
                RETURNING id",
                new { userId, amount },
                tx
            );

            await tx.CommitAsync();

            return new { status = "RESERVED", reservationId };
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<object> ReleaseAsync(Guid reservationId)
    {
        await using var conn = await dbConnectionFactory.CreateConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var reservation = await conn.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT status FROM reservations
                WHERE id = @id FOR UPDATE",
                new { id = reservationId },
                tx
            );

            if (reservation == null) throw new Exception("Reservation not found");
            if (reservation.status != "PENDING") throw new Exception($"Cannot release. Status is {reservation.status}");

            await conn.ExecuteAsync(
                @"UPDATE reservations SET status = 'REVERSED' WHERE id = @id",
                new { id = reservationId },
                tx
            );

            await tx.CommitAsync();

            return new { status = "RELEASED", reservationId };


        }
        catch
        {

            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<object> SettleAsync(Guid reservationId)
    {
        await using var conn = await dbConnectionFactory.CreateConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var reservation = await conn.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT user_id, amount, status FROM reservations
                WHERE id = @id FOR UPDATE",
                new { id = reservationId },
                tx
            );

            if (reservation == null) throw new Exception("Reservation not found");
            if (reservation.status != "PENDING") throw new Exception($"Cannot settle. Status is {reservation.status}");

            await conn.ExecuteAsync(
                @"UPDATE wallets
                SET balance = balance - @amount,
                    held_balance = held_balance - @amount
                WHERE user_id = @userId",
                new { userId = reservation.user_id, amount = reservation.amount },
                tx
            );

            await conn.ExecuteAsync(
                @"UPDATE reservations
                SET status = 'SETTLED'
                WHERE id = @id",
                new { id = reservation.id },
                tx
            );

            await tx.CommitAsync();

            return new { status = "SETTLED" };

        }
        catch
        {

            await tx.RollbackAsync();
            throw;
        }
    }
}