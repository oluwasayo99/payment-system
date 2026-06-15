namespace Demo.Ledger.Service;

public record ReserveRequest(Guid UserId, decimal Amount);

public record ReleaseRequest(Guid ReservationId);

public record SettleRequest(Guid ReservationId);