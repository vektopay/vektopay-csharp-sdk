# Vektopay C# SDK

C# SDK for Vektopay API (server-side). Supports payments, checkout sessions, and payment status polling.

## Setup

```csharp
var client = new VektopaySdk.VektopayClient(
  Environment.GetEnvironmentVariable("VEKTOPAY_API_KEY")!,
  "https://api.vektopay.com"
);
```

## Create Transaction (API Checkout)

```csharp
var transaction = await client.CreateTransactionAsync(
  new VektopaySdk.TransactionInput(
    "cust_123",
    new[] { new VektopaySdk.TransactionItemInput("price_basic", 1) },
    new VektopaySdk.TransactionPaymentMethodInput("credit_card", "ev:tk_123", 1),
    "OFF10"
  )
);
```

## Create Customer

Customers must exist before creating transactions or charges.

```csharp
var customer = await client.CreateCustomerAsync(
  new VektopaySdk.CustomerCreateInput(
    "mrc_123",
    "cust_ext_123",
    "CPF",
    "12345678901",
    "Ana Silva",
    "ana@example.com"
  )
);
```

## Update Customer

```csharp
var updated = await client.UpdateCustomerAsync(
  "cust_123",
  new VektopaySdk.CustomerUpdateInput(
    Name: "Ana Maria Silva",
    Email: "ana.maria@example.com"
  )
);
```

## Get Customer

```csharp
var detail = await client.GetCustomerAsync("cust_123");
```

## List Customers

```csharp
var customers = await client.ListCustomersAsync(
  new VektopaySdk.CustomerListParams("mrc_123", 50, 0)
);
```

## Delete Customer

```csharp
var deleted = await client.DeleteCustomerAsync("cust_123");
```

## Create Charge (Card)

```csharp
var charge = await client.CreateChargeAsync(
  new VektopaySdk.ChargeInput("cust_123", "card_123", 1000, "BRL", 1)
);
```

## Create Checkout Session (Frontend)

```csharp
var session = await client.CreateCheckoutSessionAsync(
  new VektopaySdk.CheckoutSessionInput(
    "cust_123",
    1000,
    "BRL",
    successUrl: "https://example.com/success",
    cancelUrl: "https://example.com/cancel"
  )
);
```

## Poll Charge Status

```csharp
var status = await client.PollChargeStatusAsync(charge.GetProperty("id").GetString()!);
```

## Build

```bash
dotnet build
```

## Notes
- Never expose your API key in the browser.
