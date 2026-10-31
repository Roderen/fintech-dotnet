using SharedKernel;

namespace Domain.Transactions;

public static class TransactionErrors
{
    public static Error DifferentCurrency() => Error.Conflict(
        "Wallet.DifferentCurrency",
        "Source Wallet and Target Wallets currency are different"
    );

    public static Error WalletsNotFound() => Error.NotFound(
        "Wallet.WalletsNotFound",
        "Wallets not found"
    );
    
    public static Error IdempotencyConflict() => Error.Conflict(
        "Transaction.IdempotencyConflict",
        "Idempotency key cannot be used for this request.");
}