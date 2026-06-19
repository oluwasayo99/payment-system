using Demo.Common.Contracts;
using Demo.Ledger.Service.Services;
using MassTransit;

namespace Demo.Ledger.Service.Consumers;

public class PaymentSettledConsumer : IConsumer<PaymentSettled>
{
    private readonly LedgerService ledgerService;

    public PaymentSettledConsumer(LedgerService ledgerService)
    {
        this.ledgerService = ledgerService;
    }

    public async Task Consume(ConsumeContext<PaymentSettled> context)
    {
        var message = context.Message;
        
        try
        {
            await ledgerService.SettleAsync(message.ReservtionId);
            Console.WriteLine($"Ledger settled reservation {message.ReservtionId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Ledger failed to settle reservation {message.ReservtionId}: {ex.Message}");
            throw;
        }
    }
}
