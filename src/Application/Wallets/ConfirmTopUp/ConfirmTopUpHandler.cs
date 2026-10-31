using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Payments;
using Domain.TopUps;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.ConfirmTopUp;

public sealed class ConfirmTopUpHandler(IApplicationDbContext context, IPaymentGateway gateway)
    : ICommandHandler<ConfirmTopUpCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ConfirmTopUpCommand request, CancellationToken cancellationToken)
    {
        // Network calls happen before acquiring database locks.
        var payment = await gateway.GetPaymentSession(request.SessionId, cancellationToken);
        await using var transaction = await context.BeginTransactionAsync(cancellationToken);

        // Status is an EF concurrency token: only one confirmation can commit the transition.
        var topUp = await context.TopUps.SingleOrDefaultAsync(t => t.Id == payment.TopUpId, cancellationToken);
        if (topUp is null)
            return Result.Failure<Guid>(Error.NotFound("TopUp.NotFound", "Top-up not found."));

        if ((topUp.StripeSessionId is not null && topUp.StripeSessionId != payment.SessionId) ||
            topUp.Amount * 100 != payment.AmountTotal ||
            !string.Equals(topUp.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<Guid>(TopUpErrors.IncorrectConfirmation());

        // Also covers a webhook arriving before CreateTopUp saved the session ID.
        topUp.SetStripeSessionId(payment.SessionId);
        if (topUp.Status == TopUpStatus.Succeeded)
        {
            if (payment.PaymentStatus != "paid" || topUp.StripePaymentIntentId != payment.PaymentIntentId)
                return Result.Failure<Guid>(TopUpErrors.IncorrectConfirmation());
            return topUp.Id;
        }

        if (payment.PaymentStatus == "paid")
        {
            if (string.IsNullOrWhiteSpace(payment.PaymentIntentId))
                return Result.Failure<Guid>(TopUpErrors.IncorrectConfirmation());

            var wallet = await context.Wallets.SingleAsync(w => w.Id == topUp.WalletId, cancellationToken);
            if (!string.Equals(wallet.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
                return Result.Failure<Guid>(TopUpErrors.DifferentCurrency());
            wallet.Deposit(topUp.Amount);
            topUp.Complete(payment.PaymentIntentId);
        }
        else if (payment.Status == "expired")
        {
            topUp.Expire();
        }

        // EF saves balance and status atomically. A concurrent wallet write causes a rollback;
        // the caller retries rather than overwriting a balance from an old snapshot.
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return topUp.Id;
    }
}
