using System.Text.Json.Serialization;

namespace Payment.Api.DTOs;

public sealed class CreatePaymentRequest
{
    [JsonPropertyName("from")]
    public PaymentParty From { get; init; } = new();

    [JsonPropertyName("to")]
    public PaymentParty To { get; init; } = new();

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }
}

public sealed class PaymentParty
{
    [JsonPropertyName("bank")]
    public string Bank { get; init; } = string.Empty;

    [JsonPropertyName("account")]
    public string Account { get; init; } = string.Empty;
}
