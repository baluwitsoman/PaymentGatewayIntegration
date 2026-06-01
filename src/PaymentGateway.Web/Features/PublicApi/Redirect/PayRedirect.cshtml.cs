using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Web.Domain.Entities;
using PaymentGateway.Web.Domain.Enums;
using PaymentGateway.Web.Infrastructure.Persistence;
using PaymentGateway.Web.Infrastructure.Providers;

namespace PaymentGateway.Web.Features.PublicApi.Redirect;

public class PayRedirectModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IPaymentProviderRegistry _providers;
    private readonly ILogger<PayRedirectModel> _logger;

    public PayRedirectModel(AppDbContext db, IPaymentProviderRegistry providers, ILogger<PayRedirectModel> logger)
    {
        _db = db;
        _providers = providers;
        _logger = logger;
    }

    public string? PaymentUrl { get; set; }
    public string? Message { get; set; }
    public string CompanyName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Reference { get; set; } = "";

    /// Populated when the customer hasn't yet chosen a method.
    public IReadOnlyList<MethodOption> AvailableMethods { get; set; } = Array.Empty<MethodOption>();

    public record MethodOption(Guid MethodId, PaymentProviderCode ProviderCode, string ProviderName,
        PaymentMethodType MethodType, string DisplayName, int SortOrder);

    public async Task<IActionResult> OnGetAsync(string reference, CancellationToken ct)
    {
        var order = await _db.PaymentOrders.IgnoreQueryFilters()
            .Include(o => o.Company)
            .Include(o => o.SelectedMethod)
            .FirstOrDefaultAsync(o => o.OrderReference == reference, ct);
        if (order == null) return NotFound();

        if (order.Status == PaymentOrderStatus.Paid)
            return Redirect(AppendStatus(order.SuccessReturnUrl, "paid", order.OrderReference));

        if (order.Status is PaymentOrderStatus.Failed or PaymentOrderStatus.Cancelled or PaymentOrderStatus.Expired)
            return Redirect(AppendStatus(order.FailureReturnUrl, order.Status.ToString().ToLowerInvariant(), order.OrderReference));

        if (order.ExpiresAt < DateTime.UtcNow)
        {
            order.Status = PaymentOrderStatus.Expired;
            order.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Redirect(AppendStatus(order.FailureReturnUrl, "expired", order.OrderReference));
        }

        Reference = order.OrderReference;
        CompanyName = order.Company.Name;
        Amount = order.AmountMinor / 100m;
        Currency = order.Currency;

        // If a method was pre-selected by the calling app, OR the user has already
        // chosen one and we kicked off the provider call (PaymentUrl is set),
        // forward straight to the provider's payment page.
        if (!string.IsNullOrEmpty(order.PaymentUrl))
        {
            PaymentUrl = order.PaymentUrl;
            return Page();
        }

        if (order.SelectedMethodId.HasValue)
        {
            // pre-selected by the API caller — initiate now and redirect
            return await InitiateAndRedirectAsync(order, ct);
        }

        // No selection yet — load available providers/methods for the chooser.
        var methods = await (
            from m in _db.IntegrationMethods.IgnoreQueryFilters()
            where m.CompanyId == order.CompanyId && m.IsEnabled
            join p in _db.ProviderConfigs.IgnoreQueryFilters()
                on new { m.CompanyId, m.ProviderCode } equals new { p.CompanyId, p.ProviderCode }
            where p.IsActive
            orderby p.SortOrder, m.SortOrder
            select new { m, p.DisplayLabel }
        ).ToListAsync(ct);

        AvailableMethods = methods.Select(x => new MethodOption(
            x.m.Id, x.m.ProviderCode,
            x.DisplayLabel ?? _providers.Get(x.m.ProviderCode).DisplayName,
            x.m.MethodType, x.m.DisplayName, x.m.SortOrder)).ToList();

        if (AvailableMethods.Count == 0)
        {
            Message = "No payment options are currently available. Please contact support.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string reference, Guid methodId, CancellationToken ct)
    {
        var order = await _db.PaymentOrders.IgnoreQueryFilters()
            .Include(o => o.Company)
            .FirstOrDefaultAsync(o => o.OrderReference == reference, ct);
        if (order == null) return NotFound();

        if (order.Status != PaymentOrderStatus.Created && order.Status != PaymentOrderStatus.AwaitingPayment)
            return RedirectToPage(new { reference });

        var method = await _db.IntegrationMethods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == methodId && m.CompanyId == order.CompanyId && m.IsEnabled, ct);
        if (method == null)
        {
            TempData["Error"] = "Invalid method selected.";
            return RedirectToPage(new { reference });
        }

        order.SelectedProviderCode = method.ProviderCode;
        order.SelectedMethodId = method.Id;
        order.SelectedMethod = method;
        await _db.SaveChangesAsync(ct);

        return await InitiateAndRedirectAsync(order, ct);
    }

    private async Task<IActionResult> InitiateAndRedirectAsync(PaymentOrder order, CancellationToken ct)
    {
        var method = order.SelectedMethod ?? await _db.IntegrationMethods.IgnoreQueryFilters()
            .FirstAsync(m => m.Id == order.SelectedMethodId!.Value, ct);

        var providerCode = order.SelectedProviderCode ?? method.ProviderCode;
        var cfg = await _db.ProviderConfigs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == order.CompanyId && c.ProviderCode == providerCode && c.IsActive, ct);
        if (cfg == null)
        {
            Message = "Selected payment provider is not configured.";
            order.SelectedMethodId = null;
            order.SelectedProviderCode = null;
            await _db.SaveChangesAsync(ct);
            return Page();
        }

        var customer = await _db.Customers.IgnoreQueryFilters()
            .FirstAsync(c => c.Id == order.CustomerId, ct);

        try
        {
            var provider = _providers.Get(providerCode);
            var result = await provider.InitiateAsync(
                new ProviderInitContext(cfg, method, order, customer), ct);

            order.ProviderOrderId = result.ProviderOrderId;
            order.PaymentToken = result.PaymentToken;
            order.PaymentUrl = result.RedirectUrl;
            order.Status = PaymentOrderStatus.AwaitingPayment;
            await _db.SaveChangesAsync(ct);

            return Redirect(result.RedirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {Provider} init failed for order {Ref}", providerCode, order.OrderReference);
            order.Status = PaymentOrderStatus.Failed;
            order.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Redirect(AppendStatus(order.FailureReturnUrl, "failed", order.OrderReference));
        }
    }

    private static string AppendStatus(string url, string status, string reference)
    {
        var sep = url.Contains('?') ? '&' : '?';
        return $"{url}{sep}status={status}&ref={reference}";
    }
}
