using FluentValidation;

namespace Application.Wallets.TransferFunds;

public sealed class TransferFundsCommandValidator : AbstractValidator<TransferFundsCommand>
{
    public TransferFundsCommandValidator()
    {
        RuleFor(x => x.SourceWalletId)
            .NotEmpty().WithMessage("Source wallet ID is required.");

        RuleFor(x => x.TargetWalletId)
            .NotEmpty().WithMessage("Target wallet ID is required.");

        RuleFor(x => x.SourceWalletId)
            .NotEqual(x => x.TargetWalletId).WithMessage("Source wallet ID and target wallet ID cannot be the same.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Summ of transfer must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Code of currency is required.")
            .Length(3).WithMessage("Code of currency must be 3 characters long.");
    }
}