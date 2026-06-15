namespace Demo.Common.Contracts;


public record PaymentAuthorized(
    Guid PaymentId,
    Guid UserId,
    decimal Amount,
    Guid ReservtionId
);

public record PaymentFailed(
    Guid PaymentId,
    Guid UserId,
    string Reason,
    Guid? ReservtionId
);

public record PaymentSettled(
    Guid PaymentId,
    Guid ReservationId
);

public record PaymentReleased(
    Guid PaymentId,
    Guid ReservationId
);