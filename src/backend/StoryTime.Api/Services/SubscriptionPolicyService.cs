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
    private readonly string _stateFilePath = JsonFileStateStore.ResolvePath(options.Value.Checkout.StateFilePath);
    private readonly object _persistSyncRoot = new();
    private readonly ConcurrentDictionary<string, SubscriptionState> _states = LoadStates(
        JsonFileStateStore.ResolvePath(options.Value.Checkout.StateFilePath),
        ResolveDefaultTier(options.Value));

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
            PersistStates();
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
            PersistStates();
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

            PersistStates();
            return true;
        }
    }

    public PaywallInfo GetPaywallInfo(string softUserId, int? requestedDurationMinutes = null)
    {
        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            var limits = _options.GetLimits(state.Tier);
            var nextTier = ResolvePaywallUpgradeTier(state.Tier, requestedDurationMinutes);
            return new PaywallInfo(
                CurrentTier: state.Tier,
                MaxDurationMinutes: limits.MaxDurationMinutes,
                UpgradeTier: nextTier,
                UpgradeUrl: _options.Checkout.UpgradeUrl);
        }
    }

    public CheckoutSession? CreateCheckoutSession(string softUserId, string? upgradeTier, string returnUrl, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(softUserId))
        {
            return null;
        }

        if (!TryNormalizeReturnUrl(returnUrl, out var normalizedReturnUrl))
        {
            return null;
        }

        var state = _states.GetOrAdd(softUserId, _ => new SubscriptionState(_defaultTier));
        lock (state.SyncRoot)
        {
            var targetTier = ResolveUpgradeTier(state.Tier, upgradeTier);
            if (targetTier is null)
            {
                return null;
            }

            CleanupExpiredCheckoutSessions(state, now);
            var expiresAt = now.AddMinutes(Math.Max(1, _options.Checkout.SessionTtlMinutes));
            var externalSession = TryCreateExternalCheckoutSession(
                softUserId,
                state.Tier,
                targetTier,
                normalizedReturnUrl,
                expiresAt);
            if (externalSession is not null)
            {
                state.CheckoutSessions[externalSession.SessionId] = new CheckoutSessionState(
                    externalSession.UpgradeTier,
                    externalSession.ExpiresAt,
                    IsExternalProviderSession: true);
                PersistStates();
                return externalSession;
            }

            var sessionId = Guid.NewGuid().ToString("N");
            state.CheckoutSessions[sessionId] = new CheckoutSessionState(
                targetTier,
                expiresAt,
                IsExternalProviderSession: false);

            var checkoutUrl = BuildFallbackCheckoutUrl(normalizedReturnUrl, sessionId, targetTier);
            PersistStates();
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
            PersistStates();
            return new CheckoutCompletion(CurrentTier: state.Tier, UpgradeTier: checkoutSession.UpgradeTier);
        }
    }

    private CheckoutSession? TryCreateExternalCheckoutSession(
        string softUserId,
        string currentTier,
        string targetTier,
        string callbackUrl,
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
                Content = JsonContent.Create(new CheckoutProviderCreateRequest(softUserId, currentTier, targetTier, expiresAt, callbackUrl))
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

    private void PersistStates()
    {
        lock (_persistSyncRoot)
        {
            var persisted = _states.ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    lock (pair.Value.SyncRoot)
                    {
                        return new PersistedSubscriptionState(
                            pair.Value.Tier,
                            pair.Value.CooldownUntil,
                            pair.Value.ActiveReservations.Select(reservation => reservation.ToString("N")).ToArray(),
                            pair.Value.CheckoutSessions.ToDictionary(
                                entry => entry.Key,
                                entry => new PersistedCheckoutSessionState(
                                    entry.Value.UpgradeTier,
                                    entry.Value.ExpiresAt,
                                    entry.Value.IsExternalProviderSession),
                                StringComparer.Ordinal));
                    }
                },
                StringComparer.Ordinal);

            JsonFileStateStore.Save(_stateFilePath, persisted);
        }
    }

    private static ConcurrentDictionary<string, SubscriptionState> LoadStates(string path, string defaultTier)
    {
        var persisted = JsonFileStateStore.Load(
            path,
            new Dictionary<string, PersistedSubscriptionState>(StringComparer.Ordinal));
        var states = new ConcurrentDictionary<string, SubscriptionState>(StringComparer.Ordinal);

        foreach (var (softUserId, value) in persisted)
        {
            var state = new SubscriptionState(string.IsNullOrWhiteSpace(value.Tier) ? defaultTier : value.Tier.Trim())
            {
                CooldownUntil = value.CooldownUntil
            };

            foreach (var reservation in value.ActiveReservations)
            {
                if (Guid.TryParseExact(reservation, "N", out var reservationId))
                {
                    state.ActiveReservations.Add(reservationId);
                }
            }

            foreach (var (sessionId, checkoutState) in value.CheckoutSessions)
            {
                state.CheckoutSessions[sessionId] = new CheckoutSessionState(
                    checkoutState.UpgradeTier,
                    checkoutState.ExpiresAt,
                    checkoutState.IsExternalProviderSession);
            }

            states[softUserId] = state;
        }

        return states;
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

    private string ResolvePaywallUpgradeTier(string currentTier, int? requestedDurationMinutes)
    {
        var order = _options.GetTierOrder();
        var currentIndex = order
            .Select((tier, index) => new { tier, index })
            .FirstOrDefault(entry => string.Equals(entry.tier, currentTier, StringComparison.OrdinalIgnoreCase))
            ?.index ?? -1;
        var upgradeCandidates = currentIndex < 0
            ? order.ToArray()
            : order.Skip(currentIndex + 1).ToArray();

        if (upgradeCandidates.Length == 0)
        {
            return currentTier;
        }

        if (requestedDurationMinutes is null)
        {
            return upgradeCandidates[0];
        }

        foreach (var candidate in upgradeCandidates)
        {
            if (_options.GetLimits(candidate).MaxDurationMinutes >= requestedDurationMinutes.Value)
            {
                return candidate;
            }
        }

        return upgradeCandidates[^1];
    }

    private string? ResolveUpgradeTier(string currentTier, string? requestedTier)
    {
        var nextTier = _options.GetNextTier(currentTier);
        if (string.IsNullOrWhiteSpace(nextTier) ||
            string.Equals(nextTier, currentTier, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(requestedTier))
        {
            return nextTier;
        }

        var normalizedRequestedTier = requestedTier.Trim();
        if (!_options.TierLimits.ContainsKey(normalizedRequestedTier))
        {
            return null;
        }

        return _options.IsHigherTier(currentTier, normalizedRequestedTier)
            ? normalizedRequestedTier
            : null;
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

    private static bool TryNormalizeReturnUrl(string returnUrl, out string normalizedReturnUrl)
    {
        normalizedReturnUrl = "";
        if (!Uri.TryCreate(returnUrl?.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };
        normalizedReturnUrl = builder.Uri.ToString();
        return true;
    }

    private static string BuildFallbackCheckoutUrl(string returnUrl, string sessionId, string upgradeTier)
    {
        var builder = new UriBuilder(returnUrl);
        var queryParts = builder.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("checkoutStatus=", StringComparison.OrdinalIgnoreCase) &&
                           !part.StartsWith("checkoutSessionId=", StringComparison.OrdinalIgnoreCase) &&
                           !part.StartsWith("checkoutTier=", StringComparison.OrdinalIgnoreCase))
            .ToList();
        queryParts.Add("checkoutStatus=success");
        queryParts.Add($"checkoutSessionId={Uri.EscapeDataString(sessionId)}");
        queryParts.Add($"checkoutTier={Uri.EscapeDataString(upgradeTier)}");
        builder.Query = string.Join("&", queryParts);
        builder.Fragment = string.Empty;
        return builder.Uri.ToString();
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

    private sealed record CheckoutSessionState(string UpgradeTier, DateTimeOffset ExpiresAt, bool IsExternalProviderSession);

    private sealed record CheckoutProviderCreateRequest(
        string SoftUserId,
        string CurrentTier,
        string TargetTier,
        DateTimeOffset ExpiresAt,
        string CallbackUrl);

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

    private sealed record PersistedSubscriptionState(
        string Tier,
        DateTimeOffset? CooldownUntil,
        IReadOnlyList<string> ActiveReservations,
        IReadOnlyDictionary<string, PersistedCheckoutSessionState> CheckoutSessions);

    private sealed record PersistedCheckoutSessionState(
        string UpgradeTier,
        DateTimeOffset ExpiresAt,
        bool IsExternalProviderSession);
}
