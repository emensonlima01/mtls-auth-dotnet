using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Webhook.UseCases;

namespace Webhook.Endpoints;

public static class WebhookEndopoint
{
    public static void MapWebhookEndopoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/webhook");

        group.MapPost("", async (JsonElement _, IWebhookUseCase useCase, CancellationToken cancellationToken) =>
        {
            await useCase.Handle(cancellationToken);
            return Results.Accepted();
        });
    }
}
