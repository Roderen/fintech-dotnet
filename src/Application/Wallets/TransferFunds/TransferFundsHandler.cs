using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.TransferFunds;

public class TransferFundsHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser
    ) : ICommandHandler<TransferFundsCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var userId = currentUser.UserId;
            
            var existingTransaction = await context.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, cancellationToken);
            
            if (existingTransaction != null)
            {
                var sameTransfer =
                    existingTransaction.SourceWalletId == request.SourceWalletId &&
                    existingTransaction.TargetWalletId == request.TargetWalletId &&
                    existingTransaction.Amount == request.Amount &&
                    string.Equals(
                        existingTransaction.Currency,
                        request.Currency,
                        StringComparison.OrdinalIgnoreCase);

                if (!sameTransfer)
                {
                    return Result.Failure<Guid>(
                        TransactionErrors.IdempotencyConflict());
                }

                return existingTransaction.Id;
            }
            
            var sourceWallet = await context.Wallets
                .FirstOrDefaultAsync(u => request.SourceWalletId == u.Id, cancellationToken);
            
            var  targetWallet = await context.Wallets
                .FirstOrDefaultAsync(u => u.Id == request.TargetWalletId, cancellationToken);

            if (sourceWallet == null || targetWallet == null || sourceWallet.UserId != userId)
            {
                return Result.Failure<Guid>(TransactionErrors.WalletsNotFound());
            }

            if (!sourceWallet.Currency.Equals(request.Currency, StringComparison.CurrentCultureIgnoreCase) ||
                !targetWallet.Currency.Equals(request.Currency, StringComparison.CurrentCultureIgnoreCase))
            {
                return Result.Failure<Guid>(TransactionErrors.DifferentCurrency());
            }
            
            sourceWallet.Withdraw(request.Amount);
            targetWallet.Deposit(request.Amount);
            
            var auditTransaction = new Transaction(
                sourceWallet.Id,
                targetWallet.Id,
                request.IdempotencyKey,
                request.Amount,
                request.Currency,
                "Transfer"
            );

            context.Transactions.Add(auditTransaction);
            
            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return auditTransaction.Id;
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception e)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw new Exception("An error occurred while processing your request", e);
        }
    }
}