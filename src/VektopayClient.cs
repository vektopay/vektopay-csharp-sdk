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

    public VektopayClient(string apiKey, string baseUrl, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _http = httpClient ?? new HttpClient();
    }

    public async Task<JsonElement> CreateChargeAsync(ChargeInput input)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/charges")
        {
            Content = JsonContent.Create(new
            {
                customer_id = input.CustomerId,
                card_id = input.CardId,
                amount = input.Amount,
                currency = input.Currency,
                installments = input.Installments,
                country = input.Country,
                metadata = input.Metadata,
                price_id = input.PriceId,
            })
        };
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("idempotency-key", input.IdempotencyKey ?? Guid.NewGuid().ToString());

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode)
            throw new Exception($"charge_failed_{(int)res.StatusCode}");
        return payload;
    }

    public async Task<CustomerCreateResponse> CreateCustomerAsync(CustomerCreateInput input)
    {
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
        request.Headers.Add("x-api-key", _apiKey);

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_create_failed_{(int)res.StatusCode}");

        return new CustomerCreateResponse(payload.GetProperty("id").GetString()!);
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(string id, CustomerUpdateInput input)
    {
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
        request.Headers.Add("x-api-key", _apiKey);

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
        var qs = BuildQuery(new Dictionary<string, string?>
        {
            ["merchant_id"] = query?.MerchantId,
            ["limit"] = query?.Limit?.ToString(),
            ["offset"] = query?.Offset?.ToString()
        });
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/customers{qs}");
        request.Headers.Add("x-api-key", _apiKey);

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_list_failed_{(int)res.StatusCode}");
        return payload;
    }

    public async Task<CustomerResponse> GetCustomerAsync(string id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/customers/{id}");
        request.Headers.Add("x-api-key", _apiKey);

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
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/v1/customers/{id}");
        request.Headers.Add("x-api-key", _apiKey);

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"customer_delete_failed_{(int)res.StatusCode}");

        var ok = payload.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
        return new CustomerDeleteResponse(ok);
    }

    public async Task<TransactionResponse> CreateTransactionAsync(TransactionInput input)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/transactions")
        {
            Content = JsonContent.Create(new
            {
                customer_id = input.CustomerId,
                items = input.Items.Select(item => new
                {
                    price_id = item.PriceId,
                    quantity = item.Quantity
                }),
                coupon_code = input.CouponCode,
                payment_method = new
                {
                    type = input.PaymentMethod.Type,
                    token = input.PaymentMethod.Token,
                    installments = input.PaymentMethod.Installments
                }
            })
        };
        request.Headers.Add("x-api-key", _apiKey);

        var res = await _http.SendAsync(request);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>();
        if (!res.IsSuccessStatusCode || payload.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"transaction_failed_{(int)res.StatusCode}");

        var id = payload.GetProperty("id").GetString()!;
        var status = payload.GetProperty("status").GetString()!;
        var paymentStatus = payload.TryGetProperty("paymentStatus", out var paymentStatusEl)
            ? paymentStatusEl.GetString()
            : null;
        var merchantId = payload.TryGetProperty("merchantId", out var merchantIdEl)
            ? merchantIdEl.GetString()
            : null;
        int? amount = null;
        if (payload.TryGetProperty("amount", out var amountEl) && amountEl.ValueKind == JsonValueKind.Number)
            amount = amountEl.GetInt32();
        var currency = payload.TryGetProperty("currency", out var currencyEl)
            ? currencyEl.GetString()
            : null;

        return new TransactionResponse(id, status, paymentStatus, merchantId, amount, currency);
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

        return new CheckoutSessionResponse(
            payload.GetProperty("id").GetString()!,
            payload.GetProperty("token").GetString()!,
            payload.GetProperty("expires_at").GetString()!
        );
    }

    public async Task<ChargeStatusResponse> PollChargeStatusAsync(string transactionId, TimeSpan? interval = null, TimeSpan? timeout = null)
    {
        var started = DateTime.UtcNow;
        var intervalMs = interval ?? TimeSpan.FromSeconds(3);
        var timeoutMs = timeout ?? TimeSpan.FromMinutes(2);

        while (true)
        {
            if (DateTime.UtcNow - started > timeoutMs)
                throw new Exception("poll_timeout");

            var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/charges/{transactionId}/status");
            req.Headers.Add("x-api-key", _apiKey);
            var res = await _http.SendAsync(req);
            var payload = await res.Content.ReadFromJsonAsync<ChargeStatusResponse>();
            if (!res.IsSuccessStatusCode || payload == null)
                throw new Exception($"status_failed_{(int)res.StatusCode}");
            if (payload.Status is "PAID" or "FAILED")
                return payload;
            await Task.Delay(intervalMs);
        }
    }
}
