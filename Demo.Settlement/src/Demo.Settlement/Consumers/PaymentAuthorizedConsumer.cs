using Demo.Common.Contracts;
using Demo.Common.Ledger;
using Demo.Settlement.Services;
using MassTransit;

namespace Demo.Settlement.Consumers;

public class PaymentAuthorizedConsumer : IConsumer<PaymentAuthorized>
{
    private readonly BankService bankService;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly LedgerClient ledgerClient;

    public PaymentAuthorizedConsumer(
        BankService bankService,
        LedgerClient ledgerClient,
        IPublishEndpoint publishEndpoint
    )
    {
        this.bankService = bankService;
        this.ledgerClient = ledgerClient;
        this.publishEndpoint = publishEndpoint;
    }
    public async Task Consume(ConsumeContext<PaymentAuthorized> context)
    {
        var message = context.Message;

        try
        {
            var processed = await bankService.ProcessPaymentAsync(
                message.PaymentId,
                message.Amount
            );
            Console.WriteLine($"Payment processed: {processed}");

            if (!processed)
            {
                Console.WriteLine("Payment not processed. Will release funds");
                await ledgerClient.ReleaseAsync(message.ReservtionId);
                await publishEndpoint.Publish(
                    new PaymentFailed(
                        message.PaymentId,
                        message.UserId,
                        "Bank processing failed",
                        message.ReservtionId
                    )
                );

                return;
            }

            Console.WriteLine("Payment processed. Will settle");
            await ledgerClient.SettleAsync(message.ReservtionId);
            await publishEndpoint.Publish(
                new PaymentSettled(
                    message.PaymentId,
                    message.ReservtionId
                )
            );
        }
        
        catch (Exception ex)
        {
            try
            {
                await ledgerClient.ReleaseAsync(message.ReservtionId);
                return;
            }
            catch (Exception releaseEx)
            {

                Console.WriteLine($"[CRITICAL]: Failed to release funds {releaseEx}");
            }

            await publishEndpoint.Publish(
                new PaymentFailed(
                    message.PaymentId,
                    message.UserId,
                    ex.Message,
                    message.ReservtionId
                )
            );

            throw;
        }
    }
}