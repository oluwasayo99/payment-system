using Demo.Common.Contracts;
using Demo.Common.Ledger;
using Demo.Settlement.Services;
using MassTransit;

namespace Demo.Settlement.Consumers;

public class PaymentAuthorizedConsumer : IConsumer<PaymentAuthorized>
{
    private readonly BankService bankService;
    private readonly IPublishEndpoint publishEndpoint;

    public PaymentAuthorizedConsumer(
        BankService bankService,
        IPublishEndpoint publishEndpoint
    )
    {
        this.bankService = bankService;
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
                Console.WriteLine("Payment not processed.");
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

            Console.WriteLine("Payment processed. Publishing Settlement event");
            await publishEndpoint.Publish(
                new PaymentSettled(
                    message.PaymentId,
                    message.ReservtionId
                )
            );
        }
        
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL]: Bank processing threw exception: {ex}");

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