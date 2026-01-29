namespace Webhook.UseCases;

public interface IWebhookUseCase
{
    Task Handle(CancellationToken cancellationToken = default);
}
