using FluentValidation;

namespace Application.Users.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator ()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Email is not valid.");
        
        RuleFor(x => x.Password).NotEmpty();
    }
}