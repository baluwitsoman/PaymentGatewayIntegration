using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Security;

namespace PaymentGateway.Web.Features.PublicApi.Payments;

public record CreatePaymentRequest(
    [Required] string CustomerCode,
    [Required] string CustomerName,
    string? MobileNumber,
    string? Email,
    [Range(1, long.MaxValue)] long AmountMinor,
    string? Currency,
    string? Description,
    string? ExternalReference,
    /// Optional. If specified + valid, skips the customer-side chooser and uses this provider.
    PaymentProviderCode? PreferredProvider,
    /// Optional. If specified along with PreferredProvider, uses that exact integration method.
    PaymentMethodType? PreferredMethod,
    string? SuccessUrlOverride,
    string? FailureUrlOverride,
    Dictionary<string, object>? Metadata);

public record CreatePaymentResponse(
    Guid PaymentOrderId,
    string OrderReference,
    string Status,
    string PaymentUrl,
    DateTime ExpiresAt);

public static class CreatePayment
{
    public static async Task<IResult> Handle(
        HttpContext http,
        CreatePaymentRequest req,
        AppDbContext db,
        IApiKeyGenerator keyGen,
        IConfiguration config,
        CancellationToken ct)
    {
        var auth = await ApiKeyAuth.ResolveAsync(http, db, keyGen, ct);
        if (auth == null) return Results.Unauthorized();
        var app = auth.Application;

        // Sanity check: tenant must have at least one provider+method configured.
        var hasAnyMethod = await db.IntegrationMethods.IgnoreQueryFilters()
            .AnyAsync(m => m.CompanyId == app.CompanyId && m.IsEnabled, ct);
        if (!hasAnyMethod) return Results.Problem("No payment method enabled for this company.", statusCode: 422);

        // Upsert customer
        var customer = await db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == app.CompanyId && c.CustomerCode == req.CustomerCode, ct);
        if (customer == null)
        {
            customer = new Customer
            {
                CompanyId = app.CompanyId,
                CustomerCode = req.CustomerCode,
                FullName = req.CustomerName,
                MobileNumber = req.MobileNumber,
                Email = req.Email,
            };
            db.Customers.Add(customer);
        }
        else
        {
            customer.FullName = req.CustomerName;
            if (!string.IsNullOrWhiteSpace(req.MobileNumber)) customer.MobileNumber = req.MobileNumber;
            if (!string.IsNullOrWhiteSpace(req.Email)) customer.Email = req.Email;
        }

        var orchestratorBase = config["Orchestrator:BaseUrl"]?.TrimEnd('/') ?? "";

        var order = new PaymentOrder
        {
            OrderReference = OrderReferenceGenerator.Next(),
            CompanyId = app.CompanyId,
            CompanyApplicationId = app.Id,
            CustomerId = customer.Id,
            ExternalReference = req.ExternalReference,
            Description = req.Description,
            AmountMinor = req.AmountMinor,
            Currency = (req.Currency ?? "EGP").ToUpperInvariant(),
            Status = PaymentOrderStatus.Created,
            SuccessReturnUrl = !string.IsNullOrWhiteSpace(req.SuccessUrlOverride) ? req.SuccessUrlOverride : app.SuccessReturnUrl,
            FailureReturnUrl = !string.IsNullOrWhiteSpace(req.FailureUrlOverride) ? req.FailureUrlOverride : app.FailureReturnUrl,
            Metadata = req.Metadata is null ? null : System.Text.Json.JsonSerializer.Serialize(req.Metadata),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            Customer = customer,
        };

        // Optionally pre-select provider+method so the customer skips the chooser page.
        if (req.PreferredProvider.HasValue)
        {
            var methodQ = db.IntegrationMethods.IgnoreQueryFilters()
                .Where(m => m.CompanyId == app.CompanyId
                         && m.ProviderCode == req.PreferredProvider.Value
                         && m.IsEnabled);
            var preselect = req.PreferredMethod.HasValue
                ? await methodQ.FirstOrDefaultAsync(m => m.MethodType == req.PreferredMethod.Value, ct)
                : await methodQ.OrderBy(m => m.SortOrder).FirstOrDefaultAsync(ct);
            if (preselect != null)
            {
                order.SelectedProviderCode = preselect.ProviderCode;
                order.SelectedMethodId = preselect.Id;
            }
        }

        db.PaymentOrders.Add(order);
        await db.SaveChangesAsync(ct);

        // Always return our redirect URL. The /pay/{ref} page either:
        //  - shows the provider chooser (if no SelectedMethodId), or
        //  - initiates with the chosen provider and forwards to its payment URL.
        var publicRedirect = $"{orchestratorBase}/pay/{order.OrderReference}";

        return Results.Ok(new CreatePaymentResponse(
            order.Id,
            order.OrderReference,
            order.Status.ToString(),
            publicRedirect,
            order.ExpiresAt));
    }
}
