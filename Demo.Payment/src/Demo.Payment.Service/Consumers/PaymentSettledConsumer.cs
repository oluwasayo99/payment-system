using Demo.Common;
using Demo.Common.Contracts;
using Demo.Payments.Service.Entities;
using MassTransit;

namespace Demo.Payments.Service.Consumers;

public class PaymentSettledConsumer : IConsumer<PaymentSettled>
{
    private readonly IRepository<PaymentIntent> repository;

    public PaymentSettledConsumer(IRepository<PaymentIntent> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<PaymentSettled> context)
    {
        var message = context.Message;

        var intent = await repository.GetAsync(message.PaymentId);

        if (intent == null)
        {
            Console.WriteLine($"[WARNING] PaymentIntent {message.PaymentId} not found.");
            return;
        }

        if (intent.Status == PaymentStatus.SETTLED)
        {
            return; // Already settled
        }

        intent.Status = PaymentStatus.SETTLED;
        await repository.UpdateAsync(intent);
        
        Console.WriteLine($"Payment {intent.Id} settled successfully.");
    }
}
