using Application.Abstractions.Messaging;

namespace Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Username,
    string PasswordHash,
    string Name,
    string Surname
) : ICommand<Guid>;