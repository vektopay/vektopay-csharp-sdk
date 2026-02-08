using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;

namespace VektopaySdk;

public class VektopayClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    public string? BearerToken { get; set; }

    public VektopayClient(string apiKey, string baseUrl, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _http = httpClient ?? new HttpClient();
    }

    public async Task<PaymentCreateResponse> CreatePaymentAsync(PaymentInput input)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/payments")
        {
            Content = JsonContent.Create(new
            {
                customer_id = input.CustomerId,
                customer = input.Customer == null ? null : new
                {
                    external_id = input.Customer.ExternalId,
                    name = input.Customer.Name,
                    email = input.Customer.Email,
                    doc_type = input.Customer.DocType,
                    doc_number = input.Customer.DocNumber
                },
                items = input.Items?.Select(i => new { price_id = i.PriceId, quantity = i.Quantity }),
                amount = input.Amount,
                currency = input.Currency,
                coupon_code = input.CouponCode,
                mode = input.Mode,
                webhook_url = input.WebhookUrl,
                payment_method = new
                {
                    type = input.PaymentMethod.Type,
                    token = input.PaymentMethod.Token,
                    card_id = input.PaymentMethod.CardId,
                    cvc_token = input.PaymentMethod.CvcToken,
                    installments = input.PaymentMethod.Installments
                }
            })
        };
        request.Headers.Add("x-api-key", _apiKey);

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"payment_failed_{(int)res.StatusCode}");

        var paymentId = payload.GetProperty("payment_id").GetString()!;
        var status = payload.GetProperty("status").GetString()!;
        var paymentStatus = payload.TryGetProperty("payment_status", out var psEl) ? psEl.GetString() : null;
        var subscriptionId = payload.TryGetProperty("subscription_id", out var subEl) ? subEl.GetString() : null;
        int? amount = null;
        if (payload.TryGetProperty("amount", out var amtEl) && amtEl.ValueKind == JsonValueKind.Number)
            amount = amtEl.GetInt32();
        var currency = payload.TryGetProperty("currency", out var curEl) ? curEl.GetString() : null;
        string? challengeUrl = null;
        if (payload.TryGetProperty("challenge", out var chEl) && chEl.ValueKind == JsonValueKind.Object)
        {
            challengeUrl = chEl.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;
        }

        return new PaymentCreateResponse(paymentId, status, paymentStatus, subscriptionId, amount, currency, challengeUrl);
    }

    public async Task<PaymentStatusResponse> GetPaymentStatusAsync(string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/payments/{id}/status");
        request.Headers.Add("x-api-key", _apiKey);
        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"payment_status_failed_{(int)res.StatusCode}");
        return new PaymentStatusResponse(
            payload.GetProperty("id").GetString()!,
            payload.GetProperty("status").GetString()!
        );
    }

    public async Task<PaymentStatusResponse> PollPaymentStatusAsync(string paymentId, TimeSpan? interval = null, TimeSpan? timeout = null)
    {
        var started = DateTime.UtcNow;
        var intervalMs = interval ?? TimeSpan.FromSeconds(3);
        var timeoutMs = timeout ?? TimeSpan.FromMinutes(2);

        while (true)
        {
            if (DateTime.UtcNow - started > timeoutMs)
                throw new Exception("poll_timeout");
            var status = await GetPaymentStatusAsync(paymentId);
            if (status.Status is "PAID" or "FAILED" or "CANCELED")
                return status;
            await Task.Delay(intervalMs);
        }
    }

    public async Task<JsonElement> CreateChargeAsync(ChargeInput input)
    {
        // Legacy alias: `/v1/charges` is deprecated; map to `/v1/payments`.
        var payment = await CreatePaymentAsync(new PaymentInput(
            PaymentMethod: new PaymentMethodInput(
                Type: "credit_card",
                CardId: input.CardId,
                Installments: input.Installments
            ),
            CustomerId: input.CustomerId,
            Amount: input.Amount,
            Currency: input.Currency
        ));
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            id = payment.PaymentId,
            status = payment.Status,
            challenge = payment.ChallengeUrl == null ? null : new { url = payment.ChallengeUrl, method = "redirect" }
        }));
        return doc.RootElement.Clone();
    }

    public async Task<CustomerCreateResponse> CreateCustomerAsync(CustomerCreateInput input)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            throw new Exception("bearer_token_required");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/customers")
        {
            Content = JsonContent.Create(new
            {
                merchant_id = input.MerchantId,
                external_id = input.ExternalId,
                name = input.Name,
                email = input.Email,
                doc_type = input.DocType,
                doc_number = input.DocNumber
            })
        };
        request.Headers.Add("authorization", $"Bearer {BearerToken}");

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_create_failed_{(int)res.StatusCode}");

        return new CustomerCreateResponse(payload.GetProperty("id").GetString()!);
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(string id, CustomerUpdateInput input)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            throw new Exception("bearer_token_required");
        var body = new Dictionary<string, object>();
        if (input.MerchantId != null) body["merchant_id"] = input.MerchantId;
        if (input.ExternalId != null) body["external_id"] = input.ExternalId;
        if (input.Name != null) body["name"] = input.Name;
        if (input.Email != null) body["email"] = input.Email;
        if (input.DocType != null) body["doc_type"] = input.DocType;
        if (input.DocNumber != null) body["doc_number"] = input.DocNumber;

        var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/v1/customers/{id}")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("authorization", $"Bearer {BearerToken}");

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_update_failed_{(int)res.StatusCode}");

        var merchantId = payload.TryGetProperty("merchantId", out var merchantIdEl)
            ? merchantIdEl.GetString()
            : null;
        var externalId = payload.TryGetProperty("externalId", out var externalIdEl)
            ? externalIdEl.GetString()
            : null;
        var name = payload.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString()
            : null;
        var email = payload.TryGetProperty("email", out var emailEl)
            ? emailEl.GetString()
            : null;
        var docType = payload.TryGetProperty("docType", out var docTypeEl)
            ? docTypeEl.GetString()
            : null;
        var docNumber = payload.TryGetProperty("docNumber", out var docNumberEl)
            ? docNumberEl.GetString()
            : null;
        var createdAt = payload.TryGetProperty("createdAt", out var createdAtEl)
            ? createdAtEl.GetString()
            : null;
        var updatedAt = payload.TryGetProperty("updatedAt", out var updatedAtEl)
            ? updatedAtEl.GetString()
            : null;

        return new CustomerResponse(
            payload.GetProperty("id").GetString()!,
            merchantId,
            externalId,
            name,
            email,
            docType,
            docNumber,
            createdAt,
            updatedAt
        );
    }

    public async Task<JsonElement> ListCustomersAsync(CustomerListParams? query = null)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            throw new Exception("bearer_token_required");
        var qs = BuildQuery(new Dictionary<string, string?>
        {
            ["merchant_id"] = query?.MerchantId,
            ["limit"] = query?.Limit?.ToString(),
            ["offset"] = query?.Offset?.ToString()
        });
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/customers{qs}");
        request.Headers.Add("authorization", $"Bearer {BearerToken}");

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_list_failed_{(int)res.StatusCode}");
        return payload;
    }

    public async Task<CustomerResponse> GetCustomerAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            throw new Exception("bearer_token_required");
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/customers/{id}");
        request.Headers.Add("authorization", $"Bearer {BearerToken}");

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_get_failed_{(int)res.StatusCode}");

        var merchantId = payload.TryGetProperty("merchantId", out var merchantIdEl)
            ? merchantIdEl.GetString()
            : null;
        var externalId = payload.TryGetProperty("externalId", out var externalIdEl)
            ? externalIdEl.GetString()
            : null;
        var name = payload.TryGetProperty("name", out var nameEl)
            ? nameEl.GetString()
            : null;
        var email = payload.TryGetProperty("email", out var emailEl)
            ? emailEl.GetString()
            : null;
        var docType = payload.TryGetProperty("docType", out var docTypeEl)
            ? docTypeEl.GetString()
            : null;
        var docNumber = payload.TryGetProperty("docNumber", out var docNumberEl)
            ? docNumberEl.GetString()
            : null;
        var createdAt = payload.TryGetProperty("createdAt", out var createdAtEl)
            ? createdAtEl.GetString()
            : null;
        var updatedAt = payload.TryGetProperty("updatedAt", out var updatedAtEl)
            ? updatedAtEl.GetString()
            : null;

        return new CustomerResponse(
            payload.GetProperty("id").GetString()!,
            merchantId,
            externalId,
            name,
            email,
            docType,
            docNumber,
            createdAt,
            updatedAt
        );
    }

    public async Task<CustomerDeleteResponse> DeleteCustomerAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(BearerToken))
            throw new Exception("bearer_token_required");
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/v1/customers/{id}");
        request.Headers.Add("authorization", $"Bearer {BearerToken}");

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_delete_failed_{(int)res.StatusCode}");

        var ok = payload.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
        return new CustomerDeleteResponse(ok);
    }

    public async Task<TransactionResponse> CreateTransactionAsync(TransactionInput input)
    {
        // Legacy alias: `/v1/transactions` is deprecated; map to `/v1/payments`.
        var payment = await CreatePaymentAsync(new PaymentInput(
            PaymentMethod: new PaymentMethodInput(
                Type: input.PaymentMethod.Type,
                Token: input.PaymentMethod.Token,
                Installments: input.PaymentMethod.Installments
            ),
            CustomerId: input.CustomerId,
            Items: input.Items.Select(i => new PaymentItemInput(i.PriceId, i.Quantity)).ToArray(),
            CouponCode: input.CouponCode
        ));

        return new TransactionResponse(
            payment.PaymentId,
            payment.Status,
            payment.PaymentStatus,
            null,
            payment.Amount,
            payment.Currency
        );
    }

    private static string BuildQuery(Dictionary<string, string?> values)
    {
        var parts = values
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}")
            .ToArray();

        return parts.Length == 0 ? "" : "?" + string.Join("&", parts);
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(CheckoutSessionInput input)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/checkout-sessions")
        {
            Content = JsonContent.Create(new
            {
                customer_id = input.CustomerId,
                amount = input.Amount,
                currency = input.Currency,
                price_id = input.PriceId,
                quantity = input.Quantity,
                expires_in_seconds = input.ExpiresInSeconds,
                success_url = input.SuccessUrl,
                cancel_url = input.CancelUrl,
            })
        };
        request.Headers.Add("x-api-key", _apiKey);

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"checkout_session_failed_{(int)res.StatusCode}");

        var expiresAt = payload.TryGetProperty("expires_at", out var expEl) && expEl.ValueKind == JsonValueKind.Number
            ? expEl.GetInt64()
            : long.Parse(payload.GetProperty("expires_at").GetString()!);

        return new CheckoutSessionResponse(
            payload.GetProperty("id").GetString()!,
            payload.GetProperty("token").GetString()!,
            expiresAt
        );
    }

    public async Task<ChargeStatusResponse> PollChargeStatusAsync(string transactionId, TimeSpan? interval = null, TimeSpan? timeout = null)
    {
        var status = await PollPaymentStatusAsync(transactionId, interval, timeout);
        return new ChargeStatusResponse(status.Id, status.Status);
    }
}
