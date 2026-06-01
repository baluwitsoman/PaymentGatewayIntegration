namespace PaymentGateway.Web.Features.PublicApi.Webhooks;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/webhooks").WithTags("Webhooks");
        // /webhooks/{providerCode}/{companyId} — e.g. /webhooks/Paymob/{guid}
        g.MapPost("/{providerCode}/{companyId:guid}", ProviderWebhook.Handle);
        return app;
    }
}
