using Demo.Common;
using Demo.Common.Contracts;
using Demo.Common.Ledger;
using Demo.Payments.Service.Entities;
using MassTransit;

namespace Demo.Payments.Service.Services;

public class PaymentService
{
    private readonly IRepository<PaymentIntent> paymentRepository;
    private readonly LedgerClient ledgerClient;

    private readonly IPublishEndpoint publishEndpoint;

    public PaymentService(IRepository<PaymentIntent> paymentRepository, LedgerClient ledgerClient, IPublishEndpoint publishEndpoint)
    {
        this.paymentRepository = paymentRepository;
        this.ledgerClient = ledgerClient;
        this.publishEndpoint = publishEndpoint;
    }

    public async Task<object> AuthorizeAsync(PaymentRequest request, string idempotencyKey)
    {
        PaymentIntent intent;

        try
        {
            var existing = await paymentRepository.GetAsync(x => x.IdempotencyKey == idempotencyKey);
            if (existing != null)
            {
                return new
                {

                    status = existing.Status.ToString(),
                    reservationId = existing.ReservationId
                };
            }
            intent = new PaymentIntent
            {
                UserId = request.UserId,
                Amount = request.Amount,
                Currency = request.Currency,
                IdempotencyKey = idempotencyKey
            };

            await paymentRepository.CreateAsync(intent);

            try
            {
                var reservation = await ledgerClient.ReserveAsync(
                    request.UserId,
                    request.Amount
                );

                intent.Status = PaymentStatus.AUTHORIZED;
                intent.ReservationId = reservation.ReservationId;

                await paymentRepository.UpdateAsync(intent);
                await publishEndpoint.Publish(
                    new PaymentAuthorized(
                        intent.Id,
                        intent.UserId,
                        intent.Amount,
                        intent.ReservationId
                    )
                );
                return new ReserveResponse(reservation.ReservationId, "AUTHORIZED");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mongo ERROR: {ex}");
                intent.Status = PaymentStatus.FAILED;
                intent.FailureReason = ex.Message;
                throw new Exception("Payment authorization failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Payment intent error: {ex}");
            throw new Exception(ex.Message);
        }


    }
}