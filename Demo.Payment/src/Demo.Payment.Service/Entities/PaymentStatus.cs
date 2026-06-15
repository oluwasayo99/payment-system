namespace Demo.Payments.Service.Entities;

public enum PaymentStatus
{
    PENDING,
    AUTHORIZED,
    SETTLED,
    FAILED,
    REVERSED
}