namespace Application.Abstractions.Payments;

public sealed record PaymentSession (string Id, string Url, Guid TopUpId = default);