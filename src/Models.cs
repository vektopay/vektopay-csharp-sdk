namespace VektopaySdk;

public record ChargeInput(
    string CustomerId,
    string CardId,
    int Amount,
    string Currency,
    int? Installments = null,
    string? Country = null,
    string? PriceId = null,
    Dictionary<string, object>? Metadata = null,
    string? IdempotencyKey = null
);

public record ChargeStatusResponse(string Id, string Status);

public record CheckoutSessionInput(
    string CustomerId,
    int Amount,
    string Currency,
    int? ExpiresInSeconds = null,
    string? SuccessUrl = null,
    string? CancelUrl = null
);

public record CheckoutSessionResponse(string Id, string Token, string ExpiresAt);
