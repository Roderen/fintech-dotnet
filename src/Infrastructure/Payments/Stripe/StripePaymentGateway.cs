using Application.Abstractions.Payments;
using Domain.Currencies;
using Microsoft.Extensions.Options;
using global::Stripe;
using global::Stripe.Checkout;

namespace Infrastructure.Payments.Stripe;

public sealed class StripePaymentGateway(IOptions<StripeOptions> options) : IPaymentGateway
{
    public async Task<PaymentSession> CreatePaymentSession(
        Guid topUpId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        if (topUpId == Guid.Empty)
            throw new ArgumentException("Top-up ID is required.", nameof(topUpId));

        if (amount <= 0 || amount != decimal.Round(amount, 2))
            throw new ArgumentException("Amount must be positive with at most two decimal places.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency) || !CurrencyValidator.IsSupported(currency))
            throw new ArgumentException("Only CHF, EUR and USD are supported.", nameof(currency));

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.SecretKey) ||
            !settings.SecretKey.StartsWith("sk_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Configure a Stripe Sandbox secret key in Stripe:SecretKey.");

        ValidateUrl(settings.SuccessUrl, nameof(settings.SuccessUrl));
        ValidateUrl(settings.CancelUrl, nameof(settings.CancelUrl));

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            AllowedPaymentMethodTypes = ["card"],
            ClientReferenceId = topUpId.ToString(),
            Metadata = new Dictionary<string, string> { ["topUpId"] = topUpId.ToString() },
            SuccessUrl = settings.SuccessUrl,
            CancelUrl = settings.CancelUrl,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency.ToLowerInvariant(),
                        // CHF, EUR and USD use two decimal places in Stripe.
                        UnitAmount = checked((long)(amount * 100)),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Wallet top-up"
                        }
                    }
                }
            ]
        };

        var service = new SessionService(new StripeClient(settings.SecretKey));

        // This call sends the request to Stripe; the key protects retries within Stripe's retention window.
        var session = await service.CreateAsync(
            sessionOptions,
            new RequestOptions { IdempotencyKey = $"topup:{topUpId}" },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
            throw new InvalidOperationException("Stripe did not return a Checkout URL.");

        return new PaymentSession(session.Id, session.Url, topUpId);
    }

    public async Task<PaymentDetails> GetPaymentSession(string sessionId, CancellationToken cancellationToken = default)
    {
        var key = options.Value.SecretKey;
        if (!key.StartsWith("sk_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Configure a Stripe Sandbox secret key.");

        var session = await new SessionService(new StripeClient(key))
            .GetAsync(sessionId, cancellationToken: cancellationToken);
        if (session.Livemode || session.Mode != "payment" ||
            !Guid.TryParse(session.ClientReferenceId, out var topUpId) ||
            session.Metadata.GetValueOrDefault("topUpId") != topUpId.ToString())
            throw new InvalidOperationException("Unexpected Stripe session.");

        return new PaymentDetails(session.Id, topUpId, session.AmountTotal ?? 0,
            session.Currency, session.PaymentStatus, session.Status, session.PaymentIntentId, session.Url);
    }

    private static void ValidateUrl(string value, string name)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException($"Configure an absolute HTTP or HTTPS URL in Stripe:{name}.");
    }
}
