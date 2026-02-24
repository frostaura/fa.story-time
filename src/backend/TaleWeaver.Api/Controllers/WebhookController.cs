using Microsoft.AspNetCore.Mvc;
using TaleWeaver.Api.Services;

namespace TaleWeaver.Api.Controllers;

/// <summary>
/// Stripe webhook receiver.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IStripeService stripeService,
        ILogger<WebhookController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// Receive and process Stripe webhook events.
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature");
            return BadRequest(new { error = "Missing Stripe-Signature header" });
        }

        try
        {
            await _stripeService.HandleWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, new { error = "Webhook processing failed" });
        }
    }
}
