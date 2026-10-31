using Application.Wallets.ConfirmTopUp;
using Infrastructure.Payments.Stripe;
using MediatR;
using Microsoft.Extensions.Options;
using Stripe;
using System.Text.Json;

namespace Web.Api.Endpoints;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/payments/stripe/webhook", async (
            HttpRequest request, IOptions<StripeOptions> options, ISender sender,
            ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("StripeWebhook");
            if (string.IsNullOrWhiteSpace(options.Value.WebhookSecret))
                return Results.Problem("Webhook is not configured.", statusCode: 503);

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync(ct);
            string eventId;
            string sessionId;
            try
            {
                EventUtility.ValidateSignature(
                    body, request.Headers["Stripe-Signature"].ToString(), options.Value.WebhookSecret);
                // Read only the stable event envelope. Payment fields are fetched using our SDK API version.
                using var document = JsonDocument.Parse(body);
                var envelope = document.RootElement;
                eventId = envelope.GetProperty("id").GetString()!;
                if (envelope.GetProperty("livemode").GetBoolean())
                    return Results.BadRequest("Only Sandbox events are accepted.");
                if (envelope.GetProperty("type").GetString() is not
                    ("checkout.session.completed" or "checkout.session.expired"))
                    return Results.Ok();
                var session = envelope.GetProperty("data").GetProperty("object");
                if (session.GetProperty("object").GetString() != "checkout.session")
                    return Results.BadRequest("Expected a Checkout Session.");
                sessionId = session.GetProperty("id").GetString()!;
                if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sessionId))
                    return Results.BadRequest("Missing event or session ID.");
            }
            catch (Exception ex) when (ex is StripeException or JsonException or KeyNotFoundException or InvalidOperationException)
            {
                return Results.BadRequest("Invalid Stripe event or signature.");
            }

            try
            {
                // Fetch current payment data from Stripe rather than trusting redirect parameters.
                var result = await sender.Send(new ConfirmTopUpCommand(sessionId), ct);
                if (result.IsFailure)
                {
                    logger.LogWarning("Rejected Stripe event {EventId}: {Code}", eventId, result.Error.Code);
                    return Results.BadRequest(new { code = result.Error.Code });
                }
                return Results.Ok();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process Stripe event {EventId}", eventId);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        })
            .AllowAnonymous();
    }
}
