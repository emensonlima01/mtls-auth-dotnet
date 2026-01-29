using Payment.Api.DTOs;
using Payment.Api.Infrastructure.Integration;

namespace Payment.Api.UseCases;

public sealed class CreatePaymentUseCase(IWebhookIntegration webhookIntegration) : ICreatePaymentUseCase
{
    public Task Handle(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        => webhookIntegration.SendAsync(request, cancellationToken);
}
