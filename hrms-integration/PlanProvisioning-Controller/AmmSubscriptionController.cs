
using ERPMultiTenent.Domain.Entities.Amm.Provisioning;
using ERPMultiTenent.Application.Common.Mappings;
using ERPMultiTenent.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ERPMultiTenent.WebUI.Controllers;
using ERPMultiTenent.Application.Common.Models;
using ERPMultiTenent.Application.ERP.AmmProvisioning.Payment;
using Microsoft.Extensions.Logging;


namespace ERPMultiTenent.Application.Amm ;



public class AmmSubscriptionController : ApiControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IPaymentGatewayClient _gateway;
    private readonly IAmmInvoiceRepo _invoiceRepo;       // small repo to stamp AI_ORDER_REFERENCE
    private readonly ILogger<AmmSubscriptionController> _logger;
    private readonly ICurrentUserService _currentUser;

    public AmmSubscriptionController(
        IConfiguration configuration,
        IPaymentGatewayClient gateway,
        IAmmInvoiceRepo invoiceRepo,
        ICurrentUserService currentUser,
        ILogger<AmmSubscriptionController> logger)
    {
        _configuration = configuration;
        _gateway = gateway;
        _invoiceRepo = invoiceRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet("{AS_SUBS_ID}")]
    public async Task<AmmSubscriptionDTO> Get(Int32 AS_SUBS_ID)
    {
        return await Mediator.Send(new AmmSubscriptionQuery(AS_SUBS_ID));
    }


    [HttpGet]
    public async Task<AmmSubscriptionDTOToDisp> Get()
    {
        return await Mediator.Send(new AmmSubscriptionQueryAll());
    }

    [HttpPost("Trail")]
    public async Task<ActionResult<ResultNew<AmmSubscriptionDTO>>> Trail(SubscriptionTrialCommmand command)
    {
        return await Mediator.Send(command);
    }
    [HttpPost("Purchase")]
    public async Task<ActionResult<ResultNew<AmmSubscriptionDTO>>> Purchase(PurchaseSubscriptionCommand command)
    {
        // 1. Create subscription + invoice locally (unchanged).
        var subscriptionDTO = await Mediator.Send(command);
        if (!subscriptionDTO.Succeeded) return subscriptionDTO;

        var invoiceId = command.Dto.InvoiceId;
        var amount = subscriptionDTO.ResultValue.AS_TOTAL_AMOUNT ?? 0m;

        // 2. Ask the orchestrator to create a payment order. We pass the invoice
        //    ID as `externalReference` so when the webhook comes back we can
        //    look up our local row in O(1).
        try
        {
            var gwResp = await _gateway.CreatePaymentAsync(new CreatePaymentGwRequest(
                CustomerCode:      _currentUser.CompCode,
                CustomerName:      command.Dto.AS_COMPCODE,          // or a friendlier name if available
                MobileNumber:      command.AI_PHONE_NO,
                Email:             command.AI_EMAIL_ADDRESS,
                AmountMinor:       (long)Math.Round(amount * 100m),  // EGP/OMR → minor units
                Currency:          subscriptionDTO.ResultValue.AS_CURRENCY ?? "OMR",
                Description:       command.AI_PLAN_NAME ?? $"Invoice INV-{invoiceId}",
                ExternalReference: invoiceId.ToString(),
                PreferredProvider: null,  // null → customer picks on orchestrator's chooser page
                PreferredMethod:   null,
                Metadata: new Dictionary<string, object>
                {
                    ["invoice_id"] = invoiceId,
                    ["comp_code"] = command.Dto.AS_COMPCODE ?? "",
                    ["subs_id"] = command.Dto.AS_SUBS_ID,
                }));

            // 3. Stamp the order_reference on the invoice for reconciliation.
            await _invoiceRepo.SetOrderReferenceAsync(invoiceId, gwResp.OrderReference);

            subscriptionDTO.ResultValue.RedirectURL = gwResp.PaymentUrl;
            subscriptionDTO.ResultValue.RedirectToWebsite = true;
            return subscriptionDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate payment with orchestrator for invoice {InvoiceId}", invoiceId);
            // Subscription + invoice exist locally but the payment never started.
            // Caller can retry the purchase or contact support — invoice stays unpaid.
            return ResultNew<AmmSubscriptionDTO>.Failure(
                new[] { "Could not initiate payment. Please try again or contact support." });
        }
    }


    [HttpPut]
    public async Task<ActionResult<ResultNew<AmmSubscriptionDTO>>> Put(UpdateAmmSubscriptionCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpDelete]
    public async Task<ActionResult<ResultNew<string>>> Delete(DeleteAmmSubscriptionCommand command)
    {
        return await Mediator.Send(command);
    }
}
 