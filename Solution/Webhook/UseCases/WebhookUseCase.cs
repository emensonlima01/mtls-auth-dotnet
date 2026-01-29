namespace Webhook.UseCases;

public sealed class WebhookUseCase() : IWebhookUseCase
{
    public Task Handle(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
