using MediatR;
using AutoMapper;
using FluentValidation;
using ERPMultiTenent.Application.Common.Interfaces;
using ERPMultiTenent.Application.Common.Models;
using Microsoft.Extensions.Logging;
using ERPMultiTenent.Application.Amm;

namespace ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;

/// Canonical payment event going to the HRMS Payment repo. Built either by
/// the webhook handler (after HMAC verify) or — for manual recovery — by an
/// admin tool. Always carry OrderReference; it's the idempotency key.
public class UnifiedPaymentCommand : IRequest<ResultNew<int>>
{
    public string? RawData { get; set; }
    public int InvoiceId { get; set; }
    public string TransactionId { get; set; } = "";

    /// Orchestrator's order_reference, e.g. ORD-2026-AB3X9P. The dedupe key.
    public string OrderReference { get; set; } = "";

    /// "payment.paid" / "payment.failed" — currently unused by the handler
    /// but kept for future routing.
    public string? EventType { get; set; }

    public decimal Amount { get; set; }

    /// "Paid" / "Failed" / "Cancelled" / "Expired"  (was "Y"/"N" in the old code).
    public string Status { get; set; } = "";

    public string Provider { get; set; } = "";
    public DateTime? PaidAt { get; set; }
}

public record PaymentNotificationResult
{
    public bool Redirect { get; set; }
    public string RedirectURL { get; set; } = "";
}

public class UnifiedPaymentCommandHandler : IRequestHandler<UnifiedPaymentCommand, ResultNew<int>>
{
    private readonly ILogger<UnifiedPaymentCommand> _logger;
    private readonly IAmmSubscriptionRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWorkAmm _unitOfWork;

    public UnifiedPaymentCommandHandler(
        ILogger<UnifiedPaymentCommand> logger,
        IAmmSubscriptionRepo repo,
        IMapper mapper,
        IUnitOfWorkAmm unitOfWork,
        ICurrentUserService currentUser)
    {
        _logger = logger;
        _repository = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultNew<int>> Handle(UnifiedPaymentCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.OrderReference))
        {
            _logger.LogWarning("Webhook with no OrderReference — refusing to process for invoice {Inv}", command.InvoiceId);
            return ResultNew<int>.Failure(new[] { "OrderReference required" });
        }

        _logger.LogInformation(
            "Processing webhook: Invoice={Inv} OrderRef={Ref} Status={Status} Provider={Prov} Tx={Tx}",
            command.InvoiceId, command.OrderReference, command.Status, command.Provider, command.TransactionId);

        await _unitOfWork.BeginTransactionAsync();
        int userId = await _repository.Payment(command);
        await _unitOfWork.CommitAsync();
        return ResultNew<int>.Success(userId);
    }
}
