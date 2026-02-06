# Vektopay C# SDK

MVP: charges + checkout sessions + polling.

## Usage

```csharp
var client = new VektopaySdk.VektopayClient(
  Environment.GetEnvironmentVariable("VEKTOPAY_API_KEY")!,
  "https://api.vektopay.com"
);

var session = await client.CreateCheckoutSessionAsync(
  new VektopaySdk.CheckoutSessionInput("cust_123", 1000, "BRL")
);
```

## Build

```bash
dotnet build
```
