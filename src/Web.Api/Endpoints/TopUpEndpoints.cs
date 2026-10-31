using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Payments;
using Application.Wallets.CreateTopUp;
using Application.Wallets.ConfirmTopUp;
using Domain.TopUps;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Stripe;

namespace Web.Api.Endpoints;

public static class TopUpEndpoints
{
    public sealed record CreateTopUpRequest(decimal Amount);

    public static void MapTopUpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/wallets/{walletId:guid}/top-ups", async (
            Guid walletId, CreateTopUpRequest request, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new CreateTopUpCommand(walletId, request.Amount), ct);
                return result.IsFailure ? Failure(result.Error) : Results.Ok(result.Value);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }
            catch (TopUpCheckoutException ex)
            {
                return Results.Json(new { topUpId = ex.TopUpId, error = ex.Message }, statusCode: 502);
            }
        })
        .RequireAuthorization();

        app.MapGet("/api/v1/top-ups/{id:guid}", async (Guid id, IApplicationDbContext db, CancellationToken ct) =>
        {
            var topUp = await db.TopUps.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct);
            return topUp is null ? Results.NotFound() : Results.Ok(new
            {
                topUp.Id, topUp.WalletId, topUp.Amount, topUp.Currency,
                Status = topUp.Status.ToString(), topUp.StripeSessionId,
                topUp.StripePaymentIntentId, topUp.CreatedAt, topUp.CompletedAt
            });
        })
        .RequireAuthorization();

        // Recover after a failed network request without creating a second top-up.
        app.MapPost("/api/v1/top-ups/{id:guid}/checkout", async (
            Guid id, IApplicationDbContext db, IPaymentGateway gateway, CancellationToken ct) =>
        {
            var topUp = await db.TopUps.SingleOrDefaultAsync(t => t.Id == id, ct);
            if (topUp is null) return Results.NotFound();
            if (topUp.Status != TopUpStatus.Pending) return Results.Conflict("Top-up is no longer pending.");

            PaymentSession session;
            if (topUp.StripeSessionId is not null)
            {
                var details = await gateway.GetPaymentSession(topUp.StripeSessionId, ct);
                if (details.Status != "open" || string.IsNullOrWhiteSpace(details.Url))
                    return Results.Conflict("Payment is no longer open. Synchronize its status.");
                session = new PaymentSession(details.SessionId, details.Url, topUp.Id);
            }
            else
            {
                // Stripe may prune idempotency keys after 24 hours. Do not blindly recreate old payments.
                if (DateTime.UtcNow - topUp.CreatedAt >= TimeSpan.FromHours(23))
                    return Results.Conflict("Inspect this old pending top-up in Stripe before retrying.");
                session = await gateway.CreatePaymentSession(topUp.Id, topUp.Amount, topUp.Currency, ct);
                topUp.SetStripeSessionId(session.Id);
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(session);
        })
        .RequireAuthorization();

        app.MapPost("/api/v1/top-ups/{id:guid}/sync", async (
            Guid id, IApplicationDbContext db, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var userId = currentUser.UserId;
            if (userId is null)
                return Results.Unauthorized();
            
            var topUp = await db.TopUps
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.Id == id &&
                         db.Wallets.Any(w =>
                             w.Id == t.WalletId &&
                             w.UserId == userId.Value),
                    ct);

            if (topUp is null)
                return Results.NotFound();
            
            if (topUp.StripeSessionId is null) return Results.Conflict("Recover the checkout session first.");
            var result = await sender.Send(new ConfirmTopUpCommand(topUp.StripeSessionId), ct);
            return result.IsFailure ? Failure(result.Error) : Results.Ok(new { TopUpId = result.Value });
        })
        .RequireAuthorization();
    }

    private static IResult Failure(Error error) => Results.Json(
        new { code = error.Code, message = error.Description },
        statusCode: error.Type == ErrorType.NotFound ? 404 : 400);
}
