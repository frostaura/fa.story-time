using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StoryTime.Api;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public sealed class SubscriptionPolicyService(IOptions<StoryTimeOptions> options, IHttpClientFactory httpClientFactory) : ISubscriptionPolicyService
{
    private readonly StoryTimeOptions _options = options.Value;
    private readonly MessageTemplateOptions _messages = options.Value.Messages;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _defaultTier = ResolveDefaultTier(options.Value);
    private readonly ConcurrentDictionary<string, SubscriptionState> _states = new(StringComparer.Ordinal);

    public PolicyDecision TryStartGeneration(string softUserId, int durationMinutes, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(softUserId))
        {
            return new PolicyDecision(false, StatusCodes.Status400BadRequest, _messages.SoftUserIdRequired, Guid.Empty);
        }

        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            var limits = _options.GetLimits(state.Tier);
            if (durationMinutes > limits.MaxDurationMinutes)
            {
                return new PolicyDecision(
                    false,
                    StatusCodes.Status402PaymentRequired,
                    BuildDurationTierMessage(state.Tier, limits.MaxDurationMinutes),
                    Guid.Empty);
            }

            if (state.CooldownUntil is not null && state.CooldownUntil > now)
            {
                return new PolicyDecision(false, StatusCodes.Status429TooManyRequests, _messages.SubscriptionCooldownActive, Guid.Empty);
            }

            if (state.ActiveReservations.Count >= limits.Concurrency)
            {
                return new PolicyDecision(false, StatusCodes.Status429TooManyRequests, _messages.SubscriptionConcurrencyLimitReached, Guid.Empty);
            }

            var reservationId = Guid.NewGuid();
            state.ActiveReservations.Add(reservationId);
            return new PolicyDecision(true, StatusCodes.Status200OK, _messages.SubscriptionAllowed, reservationId);
        }
    }

    public void CompleteGeneration(string softUserId, Guid reservationId, DateTimeOffset now)
    {
        if (!_states.TryGetValue(softUserId, out var state))
        {
            return;
        }

        lock (state.SyncRoot)
        {
            state.ActiveReservations.Remove(reservationId);
            var limits = _options.GetLimits(state.Tier);
            state.CooldownUntil = now.AddMinutes(limits.CooldownMinutes);
        }
    }

    public bool ApplyWebhook(string softUserId, string tier, bool resetCooldown)
    {
        if (string.IsNullOrWhiteSpace(softUserId) || string.IsNullOrWhiteSpace(tier))
        {
            return false;
        }

        if (!_options.TierLimits.ContainsKey(tier))
        {
            return false;
        }

        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            state.Tier = tier;
            if (resetCooldown)
            {
                state.CooldownUntil = null;
            }

            return true;
        }
    }

    public PaywallInfo GetPaywallInfo(string softUserId)
    {
        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            var limits = _options.GetLimits(state.Tier);
            return new PaywallInfo(
                CurrentTier: state.Tier,
                MaxDurationMinutes: limits.MaxDurationMinutes,
                UpgradeTier: _options.Checkout.UpgradeTier,
                UpgradeUrl: _options.Checkout.UpgradeUrl);
        }
    }

    public CheckoutSession? CreateCheckoutSession(string softUserId, string? upgradeTier, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(softUserId))
        {
            return null;
        }

        var targetTier = string.IsNullOrWhiteSpace(upgradeTier) ? _options.Checkout.UpgradeTier : upgradeTier.Trim();
        if (string.IsNullOrWhiteSpace(targetTier) || !_options.TierLimits.ContainsKey(targetTier))
        {
            return null;
        }

        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            CleanupExpiredCheckoutSessions(state, now);
            var externalSession = TryCreateExternalCheckoutSession(
                softUserId,
                state.Tier,
                targetTier,
                now.AddMinutes(Math.Max(1, _options.Checkout.SessionTtlMinutes)));
            if (externalSession is not null)
            {
                state.CheckoutSessions[externalSession.SessionId] = new CheckoutSessionState(
                    externalSession.UpgradeTier,
                    externalSession.ExpiresAt,
                    IsExternalProviderSession: true);
                return externalSession;
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var expiresAt = now.AddMinutes(Math.Max(1, _options.Checkout.SessionTtlMinutes));
            state.CheckoutSessions[sessionId] = new CheckoutSessionState(
                targetTier,
                expiresAt,
                IsExternalProviderSession: false);
            var checkoutUrl = $"{_options.Checkout.UpgradeUrl}?sessionId={sessionId}&user={Uri.EscapeDataString(softUserId)}";
            return new CheckoutSession(
                SessionId: sessionId,
                CurrentTier: state.Tier,
                UpgradeTier: targetTier,
                CheckoutUrl: checkoutUrl,
                ExpiresAt: expiresAt);
        }
    }

    public CheckoutCompletion? CompleteCheckoutSession(string softUserId, string sessionId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(softUserId) || string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            CleanupExpiredCheckoutSessions(state, now);
            if (!state.CheckoutSessions.TryGetValue(sessionId, out var checkoutSession) || checkoutSession.ExpiresAt <= now)
            {
                return null;
            }

            if (checkoutSession.IsExternalProviderSession &&
                !TryCompleteExternalCheckoutSession(softUserId, sessionId, checkoutSession.UpgradeTier))
            {
                return null;
            }

            state.CheckoutSessions.Remove(sessionId);
            state.Tier = checkoutSession.UpgradeTier;
            state.CooldownUntil = null;
            return new CheckoutCompletion(CurrentTier: state.Tier, UpgradeTier: checkoutSession.UpgradeTier);
        }
    }

    private CheckoutSession? TryCreateExternalCheckoutSession(
        string softUserId,
        string currentTier,
        string targetTier,
        DateTimeOffset expiresAt)
    {
        var provider = _options.Checkout.Provider;
        if (!string.Equals(provider.Mode, CheckoutProviderModes.External, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var endpoint = provider.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return provider.LocalFallbackEnabled
                ? null
                : throw new InvalidOperationException(_messages.Internal("CheckoutProviderEndpointRequiredWhenModeExternal"));
        }

        var createUrl = $"{endpoint.TrimEnd('/')}/session";
        var client = _httpClientFactory.CreateClient(nameof(SubscriptionPolicyService));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, provider.TimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, createUrl)
            {
                Content = JsonContent.Create(new CheckoutProviderCreateRequest(softUserId, currentTier, targetTier, expiresAt))
            };
            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(
                        _messages.Internal(
                            "CheckoutProviderCreateSessionFailedWithStatus",
                            ("StatusCode", ((int)response.StatusCode).ToString())));
            }

            var payload = response.Content.ReadFromJsonAsync<CheckoutProviderCreateResponse>().GetAwaiter().GetResult();
            if (payload is null ||
                string.IsNullOrWhiteSpace(payload.SessionId) ||
                string.IsNullOrWhiteSpace(payload.CheckoutUrl))
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(_messages.Internal("CheckoutProviderCreateSessionReturnedInvalidPayload"));
            }

            var providerTier = string.IsNullOrWhiteSpace(payload.UpgradeTier) ? targetTier : payload.UpgradeTier.Trim();
            if (!_options.TierLimits.ContainsKey(providerTier))
            {
                return provider.LocalFallbackEnabled
                    ? null
                    : throw new InvalidOperationException(
                        _messages.Internal(
                            "CheckoutProviderReturnedUnsupportedTier",
                            ("Tier", providerTier)));
            }

            var providerExpiresAt = payload.ExpiresAt ?? expiresAt;
            return new CheckoutSession(
                SessionId: payload.SessionId.Trim(),
                CurrentTier: currentTier,
                UpgradeTier: providerTier,
                CheckoutUrl: payload.CheckoutUrl.Trim(),
                ExpiresAt: providerExpiresAt);
        }
        catch (HttpRequestException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
        catch (TaskCanceledException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
        catch (JsonException) when (provider.LocalFallbackEnabled)
        {
            return null;
        }
    }

    private bool TryCompleteExternalCheckoutSession(string softUserId, string sessionId, string targetTier)
    {
        var provider = _options.Checkout.Provider;
        if (!string.Equals(provider.Mode, CheckoutProviderModes.External, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var endpoint = provider.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return provider.LocalFallbackEnabled
                ? true
                : throw new InvalidOperationException(_messages.Internal("CheckoutProviderEndpointRequiredWhenModeExternal"));
        }

        var completeUrl = $"{endpoint.TrimEnd('/')}/complete";
        var client = _httpClientFactory.CreateClient(nameof(SubscriptionPolicyService));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, provider.TimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, completeUrl)
            {
                Content = JsonContent.Create(new CheckoutProviderCompleteRequest(softUserId, sessionId, targetTier))
            };
            if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
            }

            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                return provider.LocalFallbackEnabled
                    ? true
                    : throw new InvalidOperationException(
                        _messages.Internal(
                            "CheckoutProviderCompletionFailedWithStatus",
                            ("StatusCode", ((int)response.StatusCode).ToString())));
            }

            var payload = response.Content.ReadFromJsonAsync<CheckoutProviderCompleteResponse>().GetAwaiter().GetResult();
            if (payload is null)
            {
                return provider.LocalFallbackEnabled
                    ? true
                    : throw new InvalidOperationException(_messages.Internal("CheckoutProviderCompletionReturnedEmptyPayload"));
            }

            if (!payload.Success)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(payload.UpgradeTier) &&
                !string.Equals(payload.UpgradeTier, targetTier, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
        catch (HttpRequestException) when (provider.LocalFallbackEnabled)
        {
            return true;
        }
        catch (TaskCanceledException) when (provider.LocalFallbackEnabled)
        {
            return true;
        }
        catch (JsonException) when (provider.LocalFallbackEnabled)
        {
            return true;
        }
    }

    private static void CleanupExpiredCheckoutSessions(SubscriptionState state, DateTimeOffset now)
    {
        foreach (var (sessionId, session) in state.CheckoutSessions.Where(entry => entry.Value.ExpiresAt <= now).ToArray())
        {
            state.CheckoutSessions.Remove(sessionId);
        }
    }

    private string BuildDurationTierMessage(string tier, int maxDurationMinutes)
    {
        return _messages.SubscriptionDurationExceedsTier
            .Replace("{Tier}", tier, StringComparison.Ordinal)
            .Replace("{MaxDurationMinutes}", maxDurationMinutes.ToString(), StringComparison.Ordinal);
    }

    private sealed class SubscriptionState
    {
        public SubscriptionState(string defaultTier)
        {
            Tier = defaultTier;
        }

        public object SyncRoot { get; } = new();

        public string Tier { get; set; }

        public DateTimeOffset? CooldownUntil { get; set; }

        public HashSet<Guid> ActiveReservations { get; } = [];

        public Dictionary<string, CheckoutSessionState> CheckoutSessions { get; } = new(StringComparer.Ordinal);
    }

    private static string ResolveDefaultTier(StoryTimeOptions options)
    {
        var defaultTier = options.Checkout.DefaultTier?.Trim();
        if (string.IsNullOrWhiteSpace(defaultTier))
        {
            throw new InvalidOperationException(options.Messages.Internal("CheckoutDefaultTierMustBeConfigured"));
        }

        if (!options.TierLimits.ContainsKey(defaultTier))
        {
            throw new InvalidOperationException(
                options.Messages.Internal(
                    "CheckoutDefaultTierMustMatchTierLimits",
                    ("Tier", defaultTier)));
        }

        return defaultTier;
    }

    private sealed record CheckoutSessionState(string UpgradeTier, DateTimeOffset ExpiresAt, bool IsExternalProviderSession);

    private sealed record CheckoutProviderCreateRequest(
        string SoftUserId,
        string CurrentTier,
        string TargetTier,
        DateTimeOffset ExpiresAt);

    private sealed record CheckoutProviderCreateResponse(
        string? SessionId,
        string? CheckoutUrl,
        DateTimeOffset? ExpiresAt,
        string? UpgradeTier);

    private sealed record CheckoutProviderCompleteRequest(
        string SoftUserId,
        string SessionId,
        string TargetTier);

    private sealed record CheckoutProviderCompleteResponse(bool Success, string? UpgradeTier);
}
