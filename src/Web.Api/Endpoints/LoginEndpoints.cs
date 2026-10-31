using Application.Users.Login;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints;

public static class LoginEndpoints
{
    public static void MapLoginEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/login", async (
                [FromBody] LoginCommand request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var login = await sender.Send(request, cancellationToken);

                    if (login.IsFailure)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.Ok(login.Value);
                }
                catch (ValidationException ex)
                {
                    return Results.ValidationProblem(
                        ex.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()));
                }
            })
            .AllowAnonymous()
            .RequireRateLimiting("LoginPolicy");

        app.MapGet("/api/v1/auth/me", (
                HttpContext context
            ) => Results.Ok(
                new
                {
                    UserId = context.User.FindFirst("sub")?.Value,
                }
            ))
            .RequireAuthorization();
    }
}