using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Wallets.ConfirmTopUp;

// Only a session ID is accepted. Payment data is fetched server-to-server.
public sealed record ConfirmTopUpCommand(string SessionId) : ICommand<Result<Guid>>;
