using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error UserNotFound() => Error.NotFound(
        "Wallet.UserNotFound",
        $"User ID not found."
    );
    
    public static Error InvalidCredentials() => Error.Failure(
        "User.InvalidCredentials",
        "Invalid credentials."
    );
}