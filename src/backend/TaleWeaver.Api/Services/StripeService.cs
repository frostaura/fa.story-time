using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using TaleWeaver.Api.Data;
using TaleWeaver.Api.Data.Models;

namespace TaleWeaver.Api.Services;

/// <summary>
/// Stripe integration for checkout sessions and webhook handling.
/// </summary>
public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly TaleWeaverDbContext _dbContext;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IConfiguration configuration,
        TaleWeaverDbContext dbContext,
        ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string softUserId, string priceId, string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                }
            ],
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "softUserId", softUserId }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation("Created checkout session {SessionId} for user {SoftUserId}",
            session.Id, softUserId);

        return session.Url;
    }

    public async Task HandleWebhookAsync(string json, string signature)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
        var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

        _logger.LogInformation("Processing Stripe event {EventType} ({EventId})",
            stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutCompleted(stripeEvent);
                break;

            case EventTypes.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdated(stripeEvent);
                break;

            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var softUserId = session.Metadata.GetValueOrDefault("softUserId");
        if (string.IsNullOrEmpty(softUserId)) return;

        var stripeSubscriptionId = session.SubscriptionId;
        var stripeCustomerId = session.CustomerId;

        // Find matching plan by Stripe price
        var subscriptionService = new Stripe.SubscriptionService();
        var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);
        var firstItem = stripeSubscription.Items.Data.FirstOrDefault();
        var priceId = firstItem?.Price?.Id;

        // Use item-level period (Stripe v50+ moved period to SubscriptionItem)
        var periodStart = firstItem?.CurrentPeriodStart;
        var periodEnd = firstItem?.CurrentPeriodEnd;

        var plan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.StripePriceId == priceId);

        if (plan == null)
        {
            _logger.LogWarning("No plan found for Stripe price {PriceId}", priceId);
            return;
        }

        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.SoftUserId == softUserId);

        if (subscription == null)
        {
            subscription = new Data.Models.Subscription
            {
                SoftUserId = softUserId,
                PlanId = plan.Id,
                StripeSubscriptionId = stripeSubscriptionId,
                StripeCustomerId = stripeCustomerId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd
            };
            _dbContext.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.PlanId = plan.Id;
            subscription.StripeSubscriptionId = stripeSubscriptionId;
            subscription.StripeCustomerId = stripeCustomerId;
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStart = periodStart;
            subscription.CurrentPeriodEnd = periodEnd;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription == null) return;

        subscription.Status = stripeSubscription.Status switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            _ => subscription.Status
        };

        // Use item-level period info
        var firstItem = stripeSubscription.Items.Data.FirstOrDefault();
        subscription.CurrentPeriodStart = firstItem?.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = firstItem?.CurrentPeriodEnd;

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Canceled;
        await _dbContext.SaveChangesAsync();
    }
}
