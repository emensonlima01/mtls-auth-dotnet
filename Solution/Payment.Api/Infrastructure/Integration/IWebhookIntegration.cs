namespace Payment.Api.Infrastructure.Integration;

public interface IWebhookIntegration
{
    Task SendAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default);
}
