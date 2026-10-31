using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Wallets.CreateWallet;

public sealed record CreateWalletCommand(
    string Currency
) : ICommand<Result<Guid>>;