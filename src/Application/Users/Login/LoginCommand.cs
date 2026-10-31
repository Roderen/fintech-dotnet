using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<Result<AccessToken>>;