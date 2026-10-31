namespace Application.Abstractions.Authentication;

public record AccessToken(
    string Token,
    DateTime ExpiresAt
);