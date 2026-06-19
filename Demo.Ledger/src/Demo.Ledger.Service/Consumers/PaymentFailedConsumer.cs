using Demo.Common.Contracts;
using Demo.Ledger.Service.Services;
using MassTransit;

namespace Demo.Ledger.Service.Consumers;

public class PaymentFailedConsumer : IConsumer<PaymentFailed>
{
    private readonly LedgerService ledgerService;

    public PaymentFailedConsumer(LedgerService ledgerService)
    {
        this.ledgerService = ledgerService;
    }

    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var message = context.Message;
        
        try
        {
            if (message.ReservationId.HasValue)
            {
                await ledgerService.ReleaseAsync(message.ReservationId.Value);
                Console.WriteLine($"Ledger released reservation {message.ReservationId.Value} due to payment failure.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Ledger failed to release reservation {message.ReservationId}: {ex.Message}");
            throw;
        }
    }
}
