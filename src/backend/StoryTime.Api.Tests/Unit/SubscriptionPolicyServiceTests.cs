using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StoryTime.Api.Services;
using System.Net;
using System.Net.Http;
using System.Text;

namespace StoryTime.Api.Tests.Unit;

public sealed class SubscriptionPolicyServiceTests
{
    private readonly SubscriptionPolicyService _service = new(
        Options.Create(StoryTimeOptionsFactory.Create()),
        new TestHttpClientFactory());

    [Fact]
    public void TryStartGeneration_EnforcesConcurrencyAndCooldown()
    {
        var now = DateTimeOffset.UtcNow;

        var first = _service.TryStartGeneration("user-limits", 5, now);
        var second = _service.TryStartGeneration("user-limits", 5, now);

        Assert.True(first.Allowed);
        Assert.False(second.Allowed);
        Assert.Equal(StatusCodes.Status429TooManyRequests, second.StatusCode);

        _service.CompleteGeneration("user-limits", first.ReservationId, now);
        var cooldownAttempt = _service.TryStartGeneration("user-limits", 5, now.AddMinutes(1));

        Assert.False(cooldownAttempt.Allowed);
        Assert.Equal(StatusCodes.Status429TooManyRequests, cooldownAttempt.StatusCode);
    }

    [Fact]
    public void TryStartGeneration_ReturnsPaymentRequiredWhenDurationExceedsTier()
    {
        var decision = _service.TryStartGeneration("user-trial", 15, DateTimeOffset.UtcNow);
        Assert.False(decision.Allowed);
        Assert.Equal(StatusCodes.Status402PaymentRequired, decision.StatusCode);
    }

    [Fact]
    public void CheckoutSession_UpgradesTierAndResetsCooldown()
    {
        var now = DateTimeOffset.UtcNow;
        var session = _service.CreateCheckoutSession("user-checkout", "Premium", now);
        Assert.NotNull(session);

        var completion = _service.CompleteCheckoutSession("user-checkout", session!.SessionId, now.AddMinutes(1));
        Assert.NotNull(completion);
        Assert.Equal("Premium", completion!.CurrentTier);

        var decision = _service.TryStartGeneration("user-checkout", 15, now.AddMinutes(2));
        Assert.True(decision.Allowed);
    }

    [Fact]
    public void TryStartGeneration_UsesConfiguredPolicyMessages()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Messages.SoftUserIdRequired = "missing-user";
        options.Messages.SubscriptionDurationExceedsTier = "tier={Tier};max={MaxDurationMinutes}";
        options.Messages.SubscriptionCooldownActive = "cooldown-active";
        options.Messages.SubscriptionConcurrencyLimitReached = "concurrency-limit";
        options.Messages.SubscriptionAllowed = "generation-allowed";

        var service = new SubscriptionPolicyService(
            Options.Create(options),
            new TestHttpClientFactory());
        var now = DateTimeOffset.UtcNow;

        var missingUserDecision = service.TryStartGeneration(string.Empty, 5, now);
        Assert.False(missingUserDecision.Allowed);
        Assert.Equal("missing-user", missingUserDecision.Message);

        var durationDecision = service.TryStartGeneration("tier-user", 15, now);
        Assert.False(durationDecision.Allowed);
        Assert.Equal("tier=Trial;max=10", durationDecision.Message);

        var first = service.TryStartGeneration("limit-user", 5, now);
        var second = service.TryStartGeneration("limit-user", 5, now);
        Assert.True(first.Allowed);
        Assert.Equal("generation-allowed", first.Message);
        Assert.False(second.Allowed);
        Assert.Equal("concurrency-limit", second.Message);

        service.CompleteGeneration("limit-user", first.ReservationId, now);
        var cooldownDecision = service.TryStartGeneration("limit-user", 5, now.AddMinutes(1));
        Assert.False(cooldownDecision.Allowed);
        Assert.Equal("cooldown-active", cooldownDecision.Message);
    }

    [Fact]
    public void CheckoutSession_UsesExternalProvider_WhenAvailable()
    {
        var service = new SubscriptionPolicyService(
            Options.Create(StoryTimeOptionsFactory.Create()),
            new TestHttpClientFactory(new SuccessfulCheckoutHandler()));
        var now = DateTimeOffset.UtcNow;

        var session = service.CreateCheckoutSession("user-external-checkout", "Premium", now);
        Assert.NotNull(session);
        Assert.StartsWith("https://payments.storytime.dev", session!.CheckoutUrl, StringComparison.Ordinal);

        var completion = service.CompleteCheckoutSession("user-external-checkout", session.SessionId, now.AddMinutes(1));
        Assert.NotNull(completion);
        Assert.Equal("Premium", completion!.UpgradeTier);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler? handler = null) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler ?? new ThrowingHandler());
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("External checkout provider is unavailable in unit tests.");
    }

    private sealed class SuccessfulCheckoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/storytime/checkout/session", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"sessionId":"ext-123","checkoutUrl":"https://payments.storytime.dev/session/ext-123","expiresAt":"2030-01-01T00:00:00Z","upgradeTier":"Premium"}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            if (path.EndsWith("/storytime/checkout/complete", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"success":true,"upgradeTier":"Premium"}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
