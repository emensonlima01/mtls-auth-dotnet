using System.Net.Http.Json;

namespace Payment.Api.Infrastructure.Integration;

public sealed class WebhookIntegration(IHttpClientFactory httpClientFactory) : IWebhookIntegration
{
    private const string WebhookClientName = "Webhook";

    public async Task SendAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(WebhookClientName);
        using var response = await client.PostAsJsonAsync("api/webhook", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
