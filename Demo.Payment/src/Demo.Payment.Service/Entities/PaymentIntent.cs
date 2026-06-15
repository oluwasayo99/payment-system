namespace Demo.Payments.Service.Entities;

using Demo.Common;


public class PaymentIntent : IEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; }

    public string IdempotencyKey { get; set; }

    public Guid ReservationId { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    public string FailureReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; }

}