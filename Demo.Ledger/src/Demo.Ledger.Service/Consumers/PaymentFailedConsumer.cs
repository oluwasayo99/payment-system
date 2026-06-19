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
            await ledgerService.ReleaseAsync(message.ReservtionId);
            Console.WriteLine($"Ledger released reservation {message.ReservtionId} due to payment failure.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Ledger failed to release reservation {message.ReservtionId}: {ex.Message}");
            throw;
        }
    }
}
