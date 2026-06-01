namespace ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;

/// Dispatches a raw webhook body to the right parser by provider key.
/// Currently we only have one — the canonical PaymentGateway orchestrator —
/// because the orchestrator already normalises across underlying providers.
/// If you ever consume a webhook from a provider DIRECTLY (bypassing the
/// orchestrator), add a handler here keyed by that provider's name.
public class PaymentProcessor
{
    private readonly Dictionary<string, IPaymentProviderHandler> _handlers;

    public PaymentProcessor()
    {
        _handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["paymentgateway"] = new PaymentGatewayHandler(),
        };
    }

    public async Task<UnifiedPaymentCommand> HandleAsync(string provider, string rawBody)
    {
        if (!_handlers.TryGetValue(provider ?? "paymentgateway", out var handler))
            handler = _handlers["paymentgateway"];   // sensible default
        return await handler.ParseAsync(rawBody);
    }
}
