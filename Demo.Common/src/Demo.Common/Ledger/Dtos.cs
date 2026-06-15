namespace Demo.Common.Ledger;

public record ReserveRequest(Guid UserId, decimal Amount);

public record ReleaseRequest(Guid ReservationId);

public record SettleRequest(Guid ReservationId);

public record ReserveResponse(Guid ReservationId, string Status);

public record ReleaseResponse(Guid ReservationId, string Status);

public record SettleResponse(string Status);