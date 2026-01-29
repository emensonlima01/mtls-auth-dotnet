using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Webhook.Endpoints;
using Webhook.Middlewares;
using Webhook.UseCases;

var builder = WebApplication.CreateBuilder(args);

var clientCertificateValidator = new ClientCertificateValidator(builder.Configuration);
builder.Services.AddSingleton(clientCertificateValidator);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        httpsOptions.ClientCertificateValidation = (certificate, _, _) =>
            certificate is not null && clientCertificateValidator.IsAllowed(certificate);
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});

builder.Services.AddOpenApi();
builder.Services.AddScoped<IWebhookUseCase, WebhookUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseClientCertificateValidation();

app.MapWebhookEndopoint();

app.Run();
