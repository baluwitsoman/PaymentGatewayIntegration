namespace ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;

/// Tiny interface so AmmSubscriptionController can stamp the orchestrator's
/// order_reference onto the invoice without depending on the full subscription repo.
/// Implement against AMM_INVOICES.
public interface IAmmInvoiceRepo
{
    Task SetOrderReferenceAsync(int invoiceId, string orderReference, CancellationToken ct = default);

    /// Lookup by orchestrator's order_reference (used by the success/failure landing pages
    /// and by the webhook receiver). Returns null if no invoice matches.
    Task<InvoiceLookupResult?> FindByOrderReferenceAsync(string orderReference, CancellationToken ct = default);
}

public record InvoiceLookupResult(
    int InvoiceId,
    int SubsId,
    int UserId,
    decimal Amount,
    string CompCode,
    string FullyPaid);
