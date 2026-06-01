using System.Text.Json.Serialization;

namespace PaymentGateway.Web.Infrastructure.Providers.Paymob;

public record PaymobAuthRequest([property: JsonPropertyName("api_key")] string ApiKey);
public record PaymobAuthResponse([property: JsonPropertyName("token")] string Token);

public record PaymobOrderRequest(
    [property: JsonPropertyName("auth_token")] string AuthToken,
    [property: JsonPropertyName("delivery_needed")] bool DeliveryNeeded,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("merchant_order_id")] string MerchantOrderId,
    [property: JsonPropertyName("items")] object[] Items);

public record PaymobOrderResponse([property: JsonPropertyName("id")] long Id);

public record PaymobBillingData(
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("phone_number")] string PhoneNumber,
    [property: JsonPropertyName("apartment")] string Apartment = "NA",
    [property: JsonPropertyName("floor")] string Floor = "NA",
    [property: JsonPropertyName("street")] string Street = "NA",
    [property: JsonPropertyName("building")] string Building = "NA",
    [property: JsonPropertyName("shipping_method")] string ShippingMethod = "NA",
    [property: JsonPropertyName("postal_code")] string PostalCode = "NA",
    [property: JsonPropertyName("city")] string City = "NA",
    [property: JsonPropertyName("country")] string Country = "EG",
    [property: JsonPropertyName("state")] string State = "NA");

public record PaymobPaymentKeyRequest(
    [property: JsonPropertyName("auth_token")] string AuthToken,
    [property: JsonPropertyName("amount_cents")] long AmountCents,
    [property: JsonPropertyName("expiration")] int ExpirationSeconds,
    [property: JsonPropertyName("order_id")] long OrderId,
    [property: JsonPropertyName("billing_data")] PaymobBillingData BillingData,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("integration_id")] long IntegrationId,
    [property: JsonPropertyName("lock_order_when_paid")] bool LockOrderWhenPaid = true);

public record PaymobPaymentKeyResponse([property: JsonPropertyName("token")] string Token);

public record PaymobInitResult(
    long PaymobOrderId,
    string PaymentToken,
    string IframeUrl);
