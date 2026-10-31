namespace Domain.TopUps;

public class TopUp
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public TopUpStatus Status { get; private set; }
    public string? StripeSessionId { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private TopUp() {}

    public void SetStripeSessionId(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (StripeSessionId is not null && StripeSessionId != sessionId)
        {
            throw new InvalidOperationException("A payment session is already assigned to this top-up.");
        }

        StripeSessionId = sessionId;
    }

    public void Complete(string paymentIntentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        if (Status == TopUpStatus.Succeeded)
        {
            if (StripePaymentIntentId != paymentIntentId)
                throw new InvalidOperationException("Top-up was completed with another payment.");
            return;
        }

        StripePaymentIntentId = paymentIntentId;
        Status = TopUpStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status == TopUpStatus.Pending)
            Status = TopUpStatus.Expired;
    }

    public TopUp(Guid walletId, decimal amount, string currency)
    {
        Id = Guid.NewGuid();
        WalletId = walletId;
        Amount = amount;
        Currency = currency;
        Status = TopUpStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
}
