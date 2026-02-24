namespace TaleWeaver.Api.Services;

/// <summary>
/// Abstracts Stripe payment operations.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Create a Stripe Checkout session and return the session URL.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        string softUserId, string priceId, string successUrl, string cancelUrl);

    /// <summary>
    /// Process a Stripe webhook event payload.
    /// </summary>
    Task HandleWebhookAsync(string json, string signature);
}
