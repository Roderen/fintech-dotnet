using Application.Abstractions.Messaging;
using Application.Users.CreateUser;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints;

public static class CreateUserEndpoints
{
    public static void MapCreateUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/users/create", async(
                [FromBody] CreateUserCommand request,
                ICommandHandler<CreateUserCommand, Guid> handler,
                CancellationToken cancellationToken,
                HttpContext httpContext) =>
            {
                var command = new CreateUserCommand(
                    request.Email,
                    request.Username,
                    request.PasswordHash,
                    request.Name,
                    request.Surname
                    );

                try
                {
                    var result = await handler.Handle(command, cancellationToken);

                    return Results.Ok(result); 
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message); 
                }
            })
            .AllowAnonymous();
    }
}