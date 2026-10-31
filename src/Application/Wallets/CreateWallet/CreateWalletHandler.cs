using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Currencies;
using Domain.Users;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.CreateWallet;

public class CreateWalletHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser
) : ICommandHandler<CreateWalletCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUser.UserId;
            
            if (userId is null)
            {
                return Result.Failure<Guid>(
                    Error.Failure("Auth.Unauthorized", "Authentication is required."));
            }
            
            if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
            {
                return Result.Failure<Guid>(WalletErrors.WrongCurrencyFormat(request.Currency));
            }

            if (!CurrencyValidator.IsSupported(request.Currency))
            {
                return Result.Failure<Guid>(WalletErrors.UnsupoprtedCurrency(request.Currency));
            }
            
            var wallet = await context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Currency == request.Currency, cancellationToken);

            if (wallet != null)
            {
                return Result.Failure<Guid>(WalletErrors.DuplicateCurrency(wallet.Currency));
            }
            
            var user = await context.Users
                .FirstOrDefaultAsync(
                    u => u.Id == userId,
                    cancellationToken);
            
            if (user is null)
            {
                return Result.Failure<Guid>(UserErrors.UserNotFound());
            }

            var newWallet = new Wallet(
                userId.Value,
                request.Currency
            );
            
            context.Wallets.Add(newWallet);
            user.MarkHasWallet(true);
            await context.SaveChangesAsync(cancellationToken);
            
            return newWallet.Id;
        }
        catch (Exception e)
        {
            throw new Exception("An error occurred while processing your request", e);
        }
    }
}