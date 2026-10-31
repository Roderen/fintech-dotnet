using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Users.CreateUser;

public class CreateUserHandler(
    IApplicationDbContext context, IPasswordHasher passwordHasher
    ) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existingUser = await context.Users
                .FirstOrDefaultAsync(t => t.Email == request.Email, cancellationToken);

            if (existingUser != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            var passwordHash = passwordHasher.Hash(request.PasswordHash);
        
            var user = new User(
                request.Email,
                request.Username,
                passwordHash,
                request.Name,
                request.Surname
            );
        
            context.Users.Add(user);
        
            await context.SaveChangesAsync(cancellationToken);
        
            return user.Id;
        } catch (Exception e) {
            throw new Exception("An error occurred while processing your request", e);
            
        }
    }
}