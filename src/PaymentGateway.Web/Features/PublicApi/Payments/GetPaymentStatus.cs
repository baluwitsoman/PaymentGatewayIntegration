using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.PublicApi.Payments;

public record PaymentStatusResponse(
    Guid PaymentOrderId,
    string OrderReference,
    string Status,
    long AmountMinor,
    string Currency,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? ExternalReference,
    string? Provider,
    string? LastTransactionId);

public static class GetPaymentStatus
{
    public static async Task<IResult> Handle(
        HttpContext http, string reference, AppDbContext db, IApiKeyGenerator keyGen, CancellationToken ct)
    {
        var auth = await ApiKeyAuth.ResolveAsync(http, db, keyGen, ct);
        if (auth == null) return Results.Unauthorized();
        var app = auth.Application;

        var order = await db.PaymentOrders.IgnoreQueryFilters()
            .Include(o => o.Transactions)
            .FirstOrDefaultAsync(o => o.OrderReference == reference && o.CompanyApplicationId == app.Id, ct);
        if (order == null) return Results.NotFound();

        var lastTx = order.Transactions.OrderByDescending(t => t.ReceivedAt).FirstOrDefault();

        return Results.Ok(new PaymentStatusResponse(
            order.Id,
            order.OrderReference,
            order.Status.ToString(),
            order.AmountMinor,
            order.Currency,
            order.CreatedAt,
            order.PaidAt,
            order.ExternalReference,
            order.SelectedProviderCode?.ToString(),
            lastTx?.ProviderTransactionId));
    }
}
