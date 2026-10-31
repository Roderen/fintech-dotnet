using System.Security.Claims;
using Application.Abstractions.Authentication;
using Application.Wallets.TransferFunds;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/wallets/transfer", async (
            [FromBody] TransferFundsCommand request,
            ISender sender,
            ICurrentUser currentUser,
            CancellationToken cancellationToken,
            HttpContext httpContext) =>
        {
            if (!httpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var extractedKey))
            {
                return Results.BadRequest("Missing X-Idempotency-Key header.");
            }

            var userId = currentUser.UserId;
            
            if (userId is null)
            {
                return Results.Unauthorized();
            }
            
            var command = request with
            {
                IdempotencyKey = extractedKey!,
                UserId = userId.Value.ToString()
            };

            try
            {
                var result = await sender.Send(command, cancellationToken);

                if (result.IsFailure)
                {
                    var body = new
                    {
                        Code = result.Error.Code,
                        Message = result.Error.Description
                    };

                    return result.Error.Type switch
                    {
                        ErrorType.Conflict => Results.Conflict(body), // 409
                        ErrorType.NotFound => Results.NotFound(body), // 404
                        _ => Results.BadRequest(body)                // 400
                    };
                }

                return Results.Ok(new { TransactionId = result.Value, Message = "Success" });
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(
                    ex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                );
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .RequireAuthorization();
    }
}