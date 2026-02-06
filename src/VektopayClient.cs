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
