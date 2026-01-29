namespace Webhook.Middlewares;

public sealed class ClientCertificateValidationMiddleware(RequestDelegate next, ClientCertificateValidator validator)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var certificate = await context.Connection.GetClientCertificateAsync();
        if (certificate is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!validator.IsAllowed(certificate))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}

public static class ClientCertificateValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseClientCertificateValidation(this IApplicationBuilder app)
        => app.UseMiddleware<ClientCertificateValidationMiddleware>();
}
