using Domain.Currencies;
using SharedKernel;

namespace Domain.Wallets;

public static class WalletErrors
{
    public static Error DuplicateCurrency(string currency) => Error.Conflict(
        "Wallet.DuplicateCurrency", 
        $"Wallet with currency {currency} already exists."
        );
    
    public static Error WrongCurrencyFormat(string currency) => Error.Failure(
        "Wallet.WrongCurrencyFormat", 
        $"Currency {currency} is not valid. It must be 3 uppercase letters."
        );
    
    public static Error UnsupoprtedCurrency(string currency) => Error.Failure(
        "Wallet.UnsupportedCurrency", 
        $"Currency {currency} is not supported. Supported currencies: {string.Join(", ", Enum.GetNames<Currency>())}"
        );
}