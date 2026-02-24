using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaleWeaver.Api.Data;
using TaleWeaver.Api.Data.Models;
using TaleWeaver.Api.Services;

namespace TaleWeaver.Api.Controllers;

/// <summary>
/// Subscription management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly TaleWeaverDbContext _dbContext;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IStripeService stripeService,
        TaleWeaverDbContext dbContext,
        ILogger<SubscriptionController> logger)
    {
        _stripeService = stripeService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a Stripe checkout session for subscription purchase.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
    {
        var softUserId = HttpContext.Items["SoftUserId"]?.ToString();
        if (string.IsNullOrEmpty(softUserId))
            return BadRequest(new { error = "SoftUserId is required" });

        var url = await _stripeService.CreateCheckoutSessionAsync(
            softUserId, request.PriceId, request.SuccessUrl, request.CancelUrl);

        return Ok(new { checkoutUrl = url });
    }

    /// <summary>
    /// Get the current subscription status for the requesting user.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var softUserId = HttpContext.Items["SoftUserId"]?.ToString();
        if (string.IsNullOrEmpty(softUserId))
            return BadRequest(new { error = "SoftUserId is required" });

        var subscription = await _dbContext.Subscriptions
            .Include(s => s.Plan)
            .ThenInclude(p => p.Tier)
            .FirstOrDefaultAsync(s => s.SoftUserId == softUserId);

        if (subscription == null)
            return Ok(new { status = "none" });

        return Ok(new
        {
            status = subscription.Status.ToString().ToLowerInvariant(),
            plan = subscription.Plan.Name,
            tier = subscription.Plan.Tier.Name,
            currentPeriodEnd = subscription.CurrentPeriodEnd,
            trialEnd = subscription.TrialEnd
        });
    }

    /// <summary>
    /// Start a 7-day trial without Stripe payment.
    /// </summary>
    [HttpPost("start-trial")]
    public async Task<IActionResult> StartTrial()
    {
        var softUserId = HttpContext.Items["SoftUserId"]?.ToString();
        if (string.IsNullOrEmpty(softUserId))
            return BadRequest(new { error = "SoftUserId is required" });

        // Check if user already has a subscription
        var existing = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.SoftUserId == softUserId);

        if (existing != null)
            return Conflict(new { error = "User already has a subscription" });

        // Find the Trial plan
        var trialPlan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Name == "Trial");

        if (trialPlan == null)
            return StatusCode(500, new { error = "Trial plan not configured" });

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            SoftUserId = softUserId,
            PlanId = trialPlan.Id,
            Status = SubscriptionStatus.Trialing,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddDays(trialPlan.TrialDays),
            TrialEnd = now.AddDays(trialPlan.TrialDays)
        };

        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Started trial for user {SoftUserId}", softUserId);

        return Ok(new
        {
            status = "trialing",
            trialEnd = subscription.TrialEnd
        });
    }
}

public class CheckoutRequest
{
    public string PriceId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
