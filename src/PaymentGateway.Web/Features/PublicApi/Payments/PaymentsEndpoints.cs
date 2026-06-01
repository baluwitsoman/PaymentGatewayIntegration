namespace PaymentGateway.Web.Features.PublicApi.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPublicPaymentApis(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1").WithTags("Payments");

        g.MapPost("/payments", CreatePayment.Handle)
            .Accepts<CreatePaymentRequest>("application/json")
            .Produces<CreatePaymentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        g.MapGet("/payments/{reference}", GetPaymentStatus.Handle)
            .Produces<PaymentStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
