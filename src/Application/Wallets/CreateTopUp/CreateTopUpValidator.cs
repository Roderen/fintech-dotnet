using FluentValidation;

namespace Application.Wallets.CreateTopUp;

public sealed class CreateTopUpValidator : AbstractValidator<CreateTopUpCommand>
{
    public CreateTopUpValidator()
    {
        RuleFor(x => x.WalletId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(999999.99m)
            .Must(amount => amount == decimal.Round(amount, 2))
            .WithMessage("Amount must have at most two decimal places.");
    }
}
