using SharedKernel;

namespace Domain.TopUps;

public class TopUpErrors
{
    public static Error WalletNotFound() => Error.NotFound(
        "Wallet.NotFound",
        "Wallet not found"
    );
    
    public static Error WrongSum() => Error.Failure(
        "TopUp.WrongSum",
        "Top up sum is not valid"
    );
    
    public static Error NotEnoughFunds() => Error.Failure(
        "TopUp.NotEnoughFunds",
        "Not enough funds in wallet"
    );
    
    public static Error DifferentCurrency() => Error.Conflict(
        "TopUp.DifferentCurrency",
        "Top up currency is different from wallet currency"
    );
    
    public static Error IncorrectConfirmation() => Error.Failure(
        "TopUp.IncorrectConfirmation",
        "Incorrect Stripe confirmation"
    );
}