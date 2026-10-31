using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Login;

public class LoginHandler (
    IApplicationDbContext context, IPasswordHasher passwordHasher, ITokenProvider tokenProvider
    ) : ICommandHandler<LoginCommand, Result<AccessToken>>
{
    public async Task<Result<AccessToken>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var findUser = await context.Users
            .AsNoTracking()
            .Where(t => t.Email == request.Email)
            .Select(u => new
            {
                Id = u.Id,
                HashedPassword = u.PasswordHash
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        var validatePassword = passwordHasher.Verify(request.Password, findUser!.HashedPassword);

        if (!validatePassword)
        {
            return Result.Failure<AccessToken>(UserErrors.InvalidCredentials());
        }

        var token = tokenProvider.CreateToken(findUser.Id);
        
        return Result.Success(token);
    }
}