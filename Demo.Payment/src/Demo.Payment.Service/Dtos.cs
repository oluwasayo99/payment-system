using System.ComponentModel.DataAnnotations;
using Demo.Payments.Service.Entities;

namespace Demo.Payments.Service;

public record PaymentIntentDto(
    Guid Id,
    Guid UserId,
    string Currency,
    PaymentStatus Status,
    string FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record PaymentRequest
(

    Guid UserId,

    [Range(0.01, 100000, ErrorMessage = "Amount exceeds card limit")]
    decimal Amount,

    [StringLength(3, MinimumLength =3)]
    string Currency,

    [MinLength(16)]
    [MaxLength(19)]
    string CardNumber

);

