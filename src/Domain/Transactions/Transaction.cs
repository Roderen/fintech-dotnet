namespace Domain.Transactions;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid? TargetWalletId { get; private set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    
    private Transaction() {}
    
    public Transaction(Guid sourceWalletId, Guid? targetWalletId, string idempotencyKey, decimal amount, string currency, string type)
    {
        Id = Guid.NewGuid();
        SourceWalletId = sourceWalletId;
        TargetWalletId = targetWalletId;
        IdempotencyKey = idempotencyKey;
        Amount = amount;
        Currency = currency.ToUpper();
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }
}