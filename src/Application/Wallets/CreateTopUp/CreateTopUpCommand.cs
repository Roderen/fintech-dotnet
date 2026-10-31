using Application.Abstractions.Messaging;
using Application.Abstractions.Payments;
using SharedKernel;

namespace Application.Wallets.CreateTopUp;

public sealed record CreateTopUpCommand(
    Guid WalletId,
    decimal Amount
    ) : ICommand<Result<PaymentSession>>;