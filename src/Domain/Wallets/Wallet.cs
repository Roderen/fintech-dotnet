namespace Domain.Wallets;

public class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    
    private Wallet() {}

    public Wallet(Guid userId, string currency)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Currency = currency;
        Balance = 0;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0", nameof(amount));
        
        Balance += amount;
    }
    
    public void Withdraw(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0", nameof(amount));
        
        if (Balance < amount) throw new ArgumentException("Insufficient funds", nameof(amount));
        
        Balance -= amount;
    }
}