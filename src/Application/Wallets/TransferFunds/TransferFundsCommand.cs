using Application.Abstractions.Idempotency;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Wallets.TransferFunds;

public sealed record TransferFundsCommand(
    Guid SourceWalletId,
    Guid TargetWalletId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string UserId
) : ICommand<Result<Guid>>, IIdempotentCommand;