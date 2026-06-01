using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;
using ERPMultiTenent.Infrastructure.DA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERPMultiTenent.Infrastructure.Repository.PPM;

/// Implementation of the small surface IAmmInvoiceRepo. Kept separate from
/// AmmSubscriptionRepo so the Purchase flow doesn't need the heavy repo.
public class AmmInvoiceRepo : IAmmInvoiceRepo
{
    private readonly IAmmDbContext _db;
    private readonly ILogger<AmmInvoiceRepo> _logger;

    public AmmInvoiceRepo(IAmmDbContext db, ILogger<AmmInvoiceRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SetOrderReferenceAsync(int invoiceId, string orderReference, CancellationToken ct = default)
    {
        await _db.AMM_INVOICES
            .Where(i => i.AI_INVOICE_ID == invoiceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AI_ORDER_REFERENCE, i => orderReference)
                .SetProperty(i => i.AI_UPDATED_DATE, i => DateTime.Now), ct);
    }

    public async Task<InvoiceLookupResult?> FindByOrderReferenceAsync(string orderReference, CancellationToken ct = default)
    {
        var inv = await _db.AMM_INVOICES.AsNoTracking()
            .Where(i => i.AI_ORDER_REFERENCE == orderReference)
            .Select(i => new
            {
                i.AI_INVOICE_ID,
                i.AI_SUBS_ID,
                i.AI_USER_ID,
                i.AI_AMOUNT,
                i.AI_FULLY_PAID,
                CompCode = _db.AMM_SUBSCRIPTIONS
                    .Where(s => s.AS_SUBS_ID == i.AI_SUBS_ID).Select(s => s.AS_COMPCODE).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (inv == null) return null;
        return new InvoiceLookupResult(
            inv.AI_INVOICE_ID,
            inv.AI_SUBS_ID ?? 0,
            inv.AI_USER_ID ?? 0,
            inv.AI_AMOUNT,
            inv.CompCode ?? "",
            inv.AI_FULLY_PAID ?? "N");
    }
}
