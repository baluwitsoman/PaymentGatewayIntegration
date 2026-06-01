// File: Controllers/PaymentTestController.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using YourHrms.Services;

namespace YourHrms.Controllers;

public class PaymentTestController : Controller
{
    private readonly IPaymentGatewayClient _gateway;
    private readonly ILogger<PaymentTestController> _logger;

    public PaymentTestController(IPaymentGatewayClient gateway, ILogger<PaymentTestController> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    // ---------- Index : show the form ----------
    [HttpGet]
    public IActionResult Index() => View(new PayFormModel());

    // ---------- Index : create order + redirect to orchestrator ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PayFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        try
        {
            var externalRef = $"HRMS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

            var resp = await _gateway.CreateAsync(new CreatePaymentRequest(
                CustomerCode: form.EmployeeId,
                CustomerName: form.EmployeeName,
                MobileNumber: form.Mobile,
                Email: form.Email,
                AmountMinor: (long)(form.AmountEgp * 100m),     // 125.00 → 12500
                Currency: "EGP",
                Description: form.Description,
                ExternalReference: externalRef,
                Metadata: new Dictionary<string, object> { ["source"] = "HRMS-Test" }
            ), ct);

            // Optional: persist the link to your DB here so you can reconcile later.
            //  await _db.PaymentRequests.AddAsync(new PaymentRequest {
            //      ExternalRef = externalRef,
            //      OrderReference = resp.OrderReference,
            //      Status = "pending",
            //      AmountMinor = (long)(form.AmountEgp * 100m),
            //      EmployeeId = form.EmployeeId,
            //  });
            //  await _db.SaveChangesAsync(ct);

            // Redirect the user's browser to the orchestrator's payment URL.
            return Redirect(resp.PaymentUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate payment");
            ModelState.AddModelError("", "Could not start payment. Please try again.");
            return View(form);
        }
    }

    // ---------- Success : user landed back from orchestrator ----------
    // The orchestrator appends ?status=paid&ref=ORD-....
    // We DON'T trust the query string. We re-fetch from the API.
    [HttpGet]
    public async Task<IActionResult> Success(string? @ref, CancellationToken ct)
    {
        var vm = new ResultViewModel { OrderReference = @ref ?? "" };
        if (string.IsNullOrEmpty(@ref)) return View(vm);

        var status = await _gateway.GetStatusAsync(@ref, ct);
        if (status == null) { vm.Message = "Payment not found."; return View("Failure", vm); }

        vm.Status = status.Status;
        vm.AmountEgp = status.AmountMinor / 100m;
        vm.PaidAt = status.PaidAt;
        vm.ExternalReference = status.ExternalReference;
        vm.Provider = status.Provider;
        vm.LastTransactionId = status.LastTransactionId;

        // If the webhook may not have arrived yet, the status will still be AwaitingPayment.
        // The view polls /PaymentTest/StatusJson for an updated state.
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Failure(string? @ref, string? status, CancellationToken ct)
    {
        var vm = new ResultViewModel { OrderReference = @ref ?? "", Status = status ?? "failed" };
        if (!string.IsNullOrEmpty(@ref))
        {
            var s = await _gateway.GetStatusAsync(@ref, ct);
            if (s != null)
            {
                vm.Status = s.Status;
                vm.AmountEgp = s.AmountMinor / 100m;
                vm.ExternalReference = s.ExternalReference;
            }
        }
        return View(vm);
    }

    // ---------- JSON endpoint for the polling spinner on Success.cshtml ----------
    [HttpGet]
    public async Task<IActionResult> StatusJson(string @ref, CancellationToken ct)
    {
        var s = await _gateway.GetStatusAsync(@ref, ct);
        if (s == null) return NotFound();
        return Json(new { status = s.Status, paidAt = s.PaidAt, txId = s.LastTransactionId });
    }
}

public class PayFormModel
{
    [Required, StringLength(50)] public string EmployeeId { get; set; } = "";
    [Required, StringLength(200)] public string EmployeeName { get; set; } = "";
    [Phone] public string? Mobile { get; set; }
    [EmailAddress] public string? Email { get; set; }
    [Required, Range(1, 1_000_000)] public decimal AmountEgp { get; set; } = 100m;
    [StringLength(500)] public string? Description { get; set; }
}

public class ResultViewModel
{
    public string OrderReference { get; set; } = "";
    public string Status { get; set; } = "AwaitingPayment";
    public decimal AmountEgp { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ExternalReference { get; set; }
    public string? Provider { get; set; }
    public string? LastTransactionId { get; set; }
    public string? Message { get; set; }
}
