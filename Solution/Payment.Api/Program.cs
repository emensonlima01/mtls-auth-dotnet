using System.Security.Cryptography.X509Certificates;
using Payment.Api.Endpoints;
using Payment.Api.Infrastructure.Integration;
using Payment.Api.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<ICreatePaymentUseCase, CreatePaymentUseCase>();
builder.Services.AddHttpClient("Webhook", client =>
{
    var baseUrl = builder.Configuration["Webhook:BaseUrl"]
                  ?? throw new InvalidOperationException("Webhook:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var certPath = builder.Configuration["Webhook:ClientCertificate:Path"]
                   ?? throw new InvalidOperationException("Webhook:ClientCertificate:Path is not configured.");
    var certPassword = builder.Configuration["Webhook:ClientCertificate:Password"]
                       ?? throw new InvalidOperationException("Webhook:ClientCertificate:Password is not configured.");

    var handler = new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual
    };
    var certificate = X509CertificateLoader.LoadPkcs12FromFile(
        certPath,
        certPassword,
        X509KeyStorageFlags.MachineKeySet);
    handler.ClientCertificates.Add(certificate);
    return handler;
});
builder.Services.AddScoped<IWebhookIntegration, WebhookIntegration>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPaymentEndopoint();

app.Run();
