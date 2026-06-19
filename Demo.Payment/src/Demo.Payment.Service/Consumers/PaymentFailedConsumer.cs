using Demo.Common;
using Demo.Common.Contracts;
using Demo.Payments.Service.Entities;
using MassTransit;

namespace Demo.Payments.Service.Consumers;

public class PaymentFailedConsumer : IConsumer<PaymentFailed>
{
    private readonly IRepository<PaymentIntent> repository;

    public PaymentFailedConsumer(IRepository<PaymentIntent> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var message = context.Message;

        var intent = await repository.GetAsync(message.PaymentId);

        if (intent == null)
        {
            Console.WriteLine($"[WARNING] PaymentIntent {message.PaymentId} not found.");
            return;
        }

        if (intent.Status == PaymentStatus.FAILED)
        {
            return;
        }

        intent.Status = PaymentStatus.FAILED;
        intent.FailureReason = message.Reason;
        await repository.UpdateAsync(intent);
        
        Console.WriteLine($"Payment {intent.Id} failed: {message.Reason}");
    }
}
