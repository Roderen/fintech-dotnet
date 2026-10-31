using Application.Abstractions.Messaging;
using Application.Wallets.CreateWallet;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Web.Api.Endpoints;

public static class CreateWalletEndpoints
{
    public static void MapCreateWalletEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/wallets/create", async (
            [FromBody] CreateWalletCommand request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new CreateWalletCommand(
                    request.Currency
                );
                
                var result = await sender.Send(command, cancellationToken);
                
                if (result.IsFailure)
                {
                    return Results.BadRequest(new { 
                        code = result.Error.Code, 
                        message = result.Error.Description 
                    });
                }
                
                return Results.Ok(result.Value);
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred while processing your request", e);
            }
        })
        .RequireAuthorization();
    }
}