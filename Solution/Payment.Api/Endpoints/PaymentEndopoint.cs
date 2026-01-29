using Microsoft.AspNetCore.Routing;
using Payment.Api.DTOs;
using Payment.Api.UseCases;

namespace Payment.Api.Endpoints;

public static class PaymentEndopoint
{
    public static void MapPaymentEndopoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/payment");

        group.MapPost("", async (
            CreatePaymentRequest request,
            ICreatePaymentUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            await useCase.Handle(request, cancellationToken);
            return Results.Accepted();
        });
    }
}
