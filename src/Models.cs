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

public record TransactionItemInput(
    string PriceId,
    int Quantity
);

public record TransactionPaymentMethodInput(
    string Type,
    string Token,
    int Installments
);

public record TransactionInput(
    string CustomerId,
    TransactionItemInput[] Items,
    TransactionPaymentMethodInput PaymentMethod,
    string? CouponCode = null
);

public record TransactionResponse(
    string Id,
    string Status,
    string? PaymentStatus = null,
    string? MerchantId = null,
    int? Amount = null,
    string? Currency = null
);

public record CustomerCreateInput(
    string MerchantId,
    string ExternalId,
    string DocType,
    string DocNumber,
    string? Name = null,
    string? Email = null
);

public record CustomerUpdateInput(
    string? MerchantId = null,
    string? ExternalId = null,
    string? Name = null,
    string? Email = null,
    string? DocType = null,
    string? DocNumber = null
);

public record CustomerCreateResponse(string Id);

public record CustomerResponse(
    string Id,
    string? MerchantId = null,
    string? ExternalId = null,
    string? Name = null,
    string? Email = null,
    string? DocType = null,
    string? DocNumber = null,
    string? CreatedAt = null,
    string? UpdatedAt = null
);

public record CustomerListParams(
    string? MerchantId = null,
    int? Limit = null,
    int? Offset = null
);

public record CustomerDeleteResponse(bool Ok);

public record CheckoutSessionInput(
    string CustomerId,
    int Amount,
    string Currency,
    int? ExpiresInSeconds = null,
    string? SuccessUrl = null,
    string? CancelUrl = null
);

public record CheckoutSessionResponse(string Id, string Token, string ExpiresAt);
