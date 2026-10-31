namespace Domain.Currencies;

public static class CurrencyValidator
{
    public static bool IsSupported(string currency)
    {
        return Enum.TryParse<Currency>(
                   currency,
                   ignoreCase: true,
                   out var result)
               && Enum.IsDefined(result);
    }
}