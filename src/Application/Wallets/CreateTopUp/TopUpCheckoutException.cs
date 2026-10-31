namespace Application.Wallets.CreateTopUp;

public sealed class TopUpCheckoutException(Guid topUpId, Exception innerException)
    : Exception("Checkout creation was interrupted. Retry this pending top-up.", innerException)
{
    public Guid TopUpId { get; } = topUpId;
}
