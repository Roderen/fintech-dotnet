using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Payments;
using Domain.TopUps;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.CreateTopUp;

public class CreateTopUpHandler(
    IApplicationDbContext context,
    IPaymentGateway paymentGateway,
    ICurrentUser currentUser
) : ICommandHandler<CreateTopUpCommand, Result<PaymentSession>>
{
    public async Task<Result<PaymentSession>> Handle(CreateTopUpCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        
        if (request.Amount <= 0 || request.Amount != decimal.Round(request.Amount, 2))
        {
            return Result.Failure<PaymentSession>(TopUpErrors.WrongSum());
        }

        var wallet = await context.Wallets
            .FirstOrDefaultAsync(
                w => w.Id == request.WalletId,
                cancellationToken);

        if (wallet is null || wallet.UserId != userId)
        {
            return Result.Failure<PaymentSession>(TopUpErrors.WalletNotFound());
        }

        var topUpRequest = new TopUp(
            wallet.Id,
            request.Amount,
            wallet.Currency
        );

        context.TopUps.Add(topUpRequest);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var session = await paymentGateway.CreatePaymentSession(
                topUpRequest.Id, topUpRequest.Amount, topUpRequest.Currency, cancellationToken);
            topUpRequest.SetStripeSessionId(session.Id);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(session);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new TopUpCheckoutException(topUpRequest.Id, ex);
        }
    }
}
