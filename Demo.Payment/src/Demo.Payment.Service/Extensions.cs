using Demo.Payments.Service.Entities;

namespace Demo.Payments.Service;

public static class Extensions
{
    public static PaymentIntentDto AsDto(this PaymentIntent paymentIntent)
    {
        return new PaymentIntentDto(
            paymentIntent.Id, 
            paymentIntent.UserId,
            paymentIntent.Currency,
            paymentIntent.Status,
            paymentIntent.FailureReason,
            paymentIntent.CreatedAt,
            paymentIntent.UpdatedAt
        );
    }
}