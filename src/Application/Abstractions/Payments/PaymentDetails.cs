namespace Application.Abstractions.Payments;

public sealed record PaymentDetails(
    string SessionId, Guid TopUpId, long AmountTotal, string Currency,
    string PaymentStatus, string Status, string? PaymentIntentId, string? Url);
