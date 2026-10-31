namespace Application.Abstractions.Payments;

public interface IPaymentGateway
{
    Task<PaymentDetails> GetPaymentSession(string sessionId, CancellationToken cancellationToken = default);

    Task<PaymentSession> CreatePaymentSession(
    Guid topUpId,
    decimal amount,
    string currency,
    CancellationToken cancellationToken = default);
}