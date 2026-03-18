using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StoryTime.Api;
using StoryTime.Api.Contracts;
using StoryTime.Api.Domain;
using StoryTime.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<StoryTimeOptions>()
    .Bind(builder.Configuration.GetSection(StoryTimeOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<StoryTimeOptions>, StoryTimeOptionsValidator>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ISubscriptionPolicyService, SubscriptionPolicyService>();
builder.Services.AddSingleton<IMediaAssetService, ProceduralMediaAssetService>();
builder.Services.AddSingleton<IStoryGenerationService, StoryGenerationService>();
var configuredMessages = builder.Configuration
    .GetSection($"{StoryTimeOptions.SectionName}:Messages")
    .Get<MessageTemplateOptions>() ?? new MessageTemplateOptions();
var corsOptions = builder.Configuration
    .GetSection($"{StoryTimeOptions.SectionName}:Cors")
    .Get<CorsOptions>() ?? throw new InvalidOperationException(configuredMessages.Internal("CorsConfigurationRequired"));
var allowedCorsOrigins = NormalizeEntries(corsOptions.AllowedOrigins);
var allowedCorsMethods = NormalizeEntries(corsOptions.AllowedMethods);
var allowedCorsHeaders = NormalizeEntries(corsOptions.AllowedHeaders);
builder.Services.AddSingleton<IStoryCatalog>(static serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<StoryTimeOptions>>().Value;
    var provider = options.Catalog.Provider.Trim();
    return provider switch
    {
        var value when string.Equals(value, CatalogProviders.InMemory, StringComparison.OrdinalIgnoreCase) =>
            ActivatorUtilities.CreateInstance<InMemoryStoryCatalog>(serviceProvider),
        var value when string.Equals(value, CatalogProviders.FileSystem, StringComparison.OrdinalIgnoreCase) =>
            ActivatorUtilities.CreateInstance<FileSystemStoryCatalog>(serviceProvider),
        _ => throw new InvalidOperationException(
            options.Messages.UnsupportedCatalogProvider
                .Replace("{Provider}", options.Catalog.Provider, StringComparison.Ordinal))
    };
});
builder.Services.AddSingleton<IParentSettingsService, ParentSettingsService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicies.Frontend, policy => policy
        .WithOrigins(allowedCorsOrigins)
        .WithMethods(allowedCorsMethods)
        .WithHeaders(allowedCorsHeaders));
});
builder.Services.AddOpenApi();

var app = builder.Build();
var apiRoutes = app.Services.GetRequiredService<IOptions<StoryTimeOptions>>().Value.ApiRoutes;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicies.Frontend);

app.MapGet(apiRoutes.HomeStatus, (IOptions<StoryTimeOptions> options) =>
{
    var ui = options.Value.Ui;
    return Results.Ok(new HomeStatusResponse(
        QuickGenerateVisible: ui.QuickGenerateVisible,
        DurationSliderVisible: ui.DurationSliderVisible,
        DurationMinMinutes: ui.DurationMinMinutes,
        DurationMaxMinutes: ui.DurationMaxMinutes,
        DurationDefaultMinutes: ui.DurationDefaultMinutes,
        DefaultChildName: ui.DefaultChildName,
        ParentControlsEnabled: ui.ParentControlsEnabled,
        DefaultTier: options.Value.Checkout.DefaultTier,
        OneShotDefaults: new OneShotDefaultsResponse(
            ArcName: options.Value.Generation.Fallbacks.ArcName,
            CompanionName: options.Value.Generation.Fallbacks.OneShotCompanionName,
            Setting: options.Value.Generation.Fallbacks.OneShotSetting,
            Mood: options.Value.Generation.Fallbacks.OneShotMood,
            ThemeTrackId: options.Value.Generation.Fallbacks.ThemeTrackId,
            NarrationStyle: options.Value.Generation.Fallbacks.NarrationStyle)));
});

app.MapPost(apiRoutes.SubscriptionWebhook, (
    HttpRequest httpRequest,
    SubscriptionWebhookRequest request,
    ISubscriptionPolicyService subscriptions,
    IOptions<StoryTimeOptions> options) =>
{
    if (!IsWebhookAuthorized(httpRequest, options.Value.Checkout.WebhookSharedSecret))
    {
        return Results.Unauthorized();
    }

    var applied = subscriptions.ApplyWebhook(request.SoftUserId, request.Tier, request.ResetCooldown);
    return applied
        ? Results.Ok()
        : Results.BadRequest(new { error = options.Value.Messages.InvalidSubscriptionPayload });
});

app.MapGet(apiRoutes.SubscriptionPaywall, (string softUserId, ISubscriptionPolicyService subscriptions, IOptions<StoryTimeOptions> options) =>
{
    var paywall = subscriptions.GetPaywallInfo(softUserId);
    return Results.Ok(new SubscriptionPaywallResponse(
        CurrentTier: paywall.CurrentTier,
        UpgradeTier: paywall.UpgradeTier,
        MaxDurationMinutes: paywall.MaxDurationMinutes,
        UpgradeUrl: paywall.UpgradeUrl,
        Message: BuildUpgradeMessage(options.Value, paywall.UpgradeTier)));
});

app.MapPost(apiRoutes.SubscriptionCheckoutSession, (
    string softUserId,
    SubscriptionCheckoutSessionRequest request,
    IParentSettingsService parentSettings,
    ISubscriptionPolicyService subscriptions,
    IOptions<StoryTimeOptions> options) =>
{
    if (!parentSettings.IsGateAuthorized(softUserId, request.GateToken, DateTimeOffset.UtcNow))
    {
        return Results.Unauthorized();
    }

    var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl)
        ? options.Value.Checkout.DefaultReturnUrl
        : request.ReturnUrl;
    var session = subscriptions.CreateCheckoutSession(softUserId, request.UpgradeTier, returnUrl, DateTimeOffset.UtcNow);
    return session is null
        ? Results.BadRequest(new { error = options.Value.Messages.UnableToCreateCheckoutSession })
        : Results.Ok(new SubscriptionCheckoutSessionResponse(
            SessionId: session.SessionId,
            CurrentTier: session.CurrentTier,
            UpgradeTier: session.UpgradeTier,
            CheckoutUrl: session.CheckoutUrl,
            ExpiresAt: session.ExpiresAt));
});

app.MapPost(apiRoutes.SubscriptionCheckoutComplete, (
    string softUserId,
    SubscriptionCheckoutCompleteRequest request,
    IParentSettingsService parentSettings,
    ISubscriptionPolicyService subscriptions,
    IOptions<StoryTimeOptions> options) =>
{
    if (!parentSettings.IsGateAuthorized(softUserId, request.GateToken, DateTimeOffset.UtcNow))
    {
        return Results.Unauthorized();
    }

    var completion = subscriptions.CompleteCheckoutSession(softUserId, request.SessionId, DateTimeOffset.UtcNow);
    return completion is null
        ? Results.BadRequest(new { error = options.Value.Messages.InvalidOrExpiredCheckoutSession })
        : Results.Ok(new SubscriptionCheckoutCompleteResponse(
            CurrentTier: completion.CurrentTier,
            UpgradeTier: completion.UpgradeTier));
});

app.MapPost(apiRoutes.ParentGateRegister, (
    string softUserId,
    ParentCredentialRegisterRequest request,
    IParentSettingsService parentSettings,
    IOptions<StoryTimeOptions> options) =>
{
    if (string.IsNullOrWhiteSpace(softUserId))
    {
        return Results.BadRequest(new { error = options.Value.Messages.SoftUserIdRequired });
    }

    var registered = parentSettings.RegisterCredential(softUserId, request.CredentialId, request.PublicKey);
    return registered
        ? Results.Ok()
        : Results.BadRequest(new { error = options.Value.Messages.InvalidParentCredential });
});

app.MapPost(apiRoutes.ParentGateChallenge, (
    string softUserId,
    IParentSettingsService parentSettings,
    IOptions<StoryTimeOptions> options) =>
{
    if (string.IsNullOrWhiteSpace(softUserId))
    {
        return Results.BadRequest(new { error = options.Value.Messages.SoftUserIdRequired });
    }

    var challenge = parentSettings.CreateChallenge(softUserId, DateTimeOffset.UtcNow);
    return Results.Ok(new ParentGateChallengeResponse(
        challenge.ChallengeId,
        challenge.Challenge,
        challenge.RpId,
        challenge.ExpiresAt));
});

app.MapPost(apiRoutes.ParentGateVerify, (string softUserId, ParentGateVerifyRequest request, IParentSettingsService parentSettings) =>
{
    var session = parentSettings.VerifyGate(
        softUserId,
        request.ChallengeId,
        request.Assertion,
        DateTimeOffset.UtcNow);

    return session is null
        ? Results.Unauthorized()
        : Results.Ok(new ParentGateVerifyResponse(session.GateToken, session.ExpiresAt));
});

app.MapGet(apiRoutes.ParentSettings, (string softUserId, HttpRequest httpRequest, IParentSettingsService parentSettings) =>
{
    var gateToken = ResolveGateToken(httpRequest);
    var settings = parentSettings.GetSettings(softUserId, gateToken, DateTimeOffset.UtcNow);
    return settings is null
        ? Results.Unauthorized()
        : Results.Ok(new ParentSettingsResponse(settings.NotificationsEnabled, settings.AnalyticsEnabled, settings.KidShelfEnabled));
});

app.MapPut(apiRoutes.ParentSettings, (string softUserId, ParentSettingsUpdateRequest request, IParentSettingsService parentSettings) =>
{
    var settings = parentSettings.UpdateSettings(
        softUserId,
        request.GateToken,
        request.NotificationsEnabled,
        request.AnalyticsEnabled,
        request.KidShelfEnabled,
        DateTimeOffset.UtcNow);

    return settings is null
        ? Results.Unauthorized()
        : Results.Ok(new ParentSettingsResponse(settings.NotificationsEnabled, settings.AnalyticsEnabled, settings.KidShelfEnabled));
});

app.MapPost(apiRoutes.StoriesGenerate, async (
    GenerateStoryRequest request,
    IStoryGenerationService storyGeneration,
    ISubscriptionPolicyService subscriptions,
    IStoryCatalog catalog,
    IOptions<StoryTimeOptions> optionsAccessor,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (request.DurationMinutes <= 0)
    {
        return Results.BadRequest(new { error = optionsAccessor.Value.Messages.DurationMinutesMustBeGreaterThanZero });
    }

    logger.LogInformation(
        "Story generation requested for user {UserHash}. mode={Mode}, duration={Duration}, reducedMotion={ReducedMotion}",
        IdentifierHashing.HashIdentifier(
            request.SoftUserId,
            optionsAccessor.Value.Catalog.HashedIdentifierByteLength,
            optionsAccessor.Value.Catalog.AnonymousIdentifierFallback),
        request.Mode,
        request.DurationMinutes,
        request.ReducedMotion);

    var decision = subscriptions.TryStartGeneration(request.SoftUserId, request.DurationMinutes, DateTimeOffset.UtcNow);
    if (!decision.Allowed)
    {
        if (decision.StatusCode == StatusCodes.Status402PaymentRequired)
        {
            var paywall = subscriptions.GetPaywallInfo(request.SoftUserId, request.DurationMinutes);
            return Results.Json(
                new
                {
                    error = decision.Message,
                    paywall = new SubscriptionPaywallResponse(
                        CurrentTier: paywall.CurrentTier,
                        UpgradeTier: paywall.UpgradeTier,
                        MaxDurationMinutes: paywall.MaxDurationMinutes,
                        UpgradeUrl: paywall.UpgradeUrl,
                        Message: BuildUpgradeMessage(optionsAccessor.Value, paywall.UpgradeTier))
                },
                statusCode: StatusCodes.Status402PaymentRequired);
        }

        return Results.Json(new { error = decision.Message }, statusCode: decision.StatusCode);
    }

    try
    {
        var generated = await storyGeneration.GenerateAsync(request, cancellationToken);
        catalog.Add(request.SoftUserId, new StoryLibraryItem
        {
            StoryId = generated.StoryId,
            Title = BuildLibraryTitle(generated, optionsAccessor.Value),
            Mode = generated.Mode,
            SeriesId = generated.SeriesId,
            IsFavorite = request.Favorite,
            FullAudioReady = !generated.ApprovalRequired,
            CreatedAt = generated.GeneratedAt
        });

        return Results.Ok(new GenerateStoryResponse(
            StoryId: generated.StoryId,
            Title: generated.Title,
            Mode: generated.Mode,
            SeriesId: generated.SeriesId,
            Recap: generated.Recap,
            Scenes: generated.Scenes,
            SceneCount: generated.Scenes.Count,
            PosterLayers: generated.PosterLayers,
            ApprovalRequired: generated.ApprovalRequired,
            TeaserAudio: generated.TeaserAudio,
            FullAudio: generated.FullAudio,
            FullAudioReady: !generated.ApprovalRequired,
            StoryBible: generated.StoryBible,
            ReducedMotion: generated.ReducedMotion,
            GeneratedAt: generated.GeneratedAt));
    }
    finally
    {
        subscriptions.CompleteGeneration(request.SoftUserId, decision.ReservationId, DateTimeOffset.UtcNow);
    }
});

app.MapPost(apiRoutes.StoryApprove, (
    string storyId,
    StoryApprovalRequest request,
    IParentSettingsService parentSettings,
    IStoryCatalog catalog) =>
{
    if (!parentSettings.IsGateAuthorized(request.SoftUserId, request.GateToken, DateTimeOffset.UtcNow))
    {
        return Results.Unauthorized();
    }

    var approved = catalog.SetApproval(request.SoftUserId, storyId);
    return approved is null
        ? Results.NotFound()
        : Results.Ok(new StoryApprovalResponse(FullAudioReady: true, FullAudio: approved.FullAudio));
});

app.MapPut(apiRoutes.StoryFavorite, (string storyId, SetFavoriteRequest request, IStoryCatalog catalog) =>
{
    var updated = catalog.SetFavorite(storyId, request.IsFavorite);
    return updated ? Results.Ok() : Results.NotFound();
});

app.MapGet(apiRoutes.Library, (string softUserId, IStoryCatalog catalog, IParentSettingsService parentSettings, IOptions<StoryTimeOptions> options) =>
{
    var kidShelfEnabled = parentSettings.IsKidShelfEnabled(softUserId);
    IReadOnlyList<StoryLibraryItem> recent = catalog.GetRecent(softUserId);
    IReadOnlyList<StoryLibraryItem> favorites = catalog.GetFavorites(softUserId);
    if (kidShelfEnabled)
    {
        recent = recent.Take(Math.Max(1, options.Value.Catalog.KidShelfRecentLimit)).ToArray();
        favorites = favorites.Take(Math.Max(1, options.Value.Catalog.KidShelfFavoritesLimit)).ToArray();
    }

    return Results.Ok(new LibraryResponse(recent, favorites, kidShelfEnabled));
});

app.MapGet(apiRoutes.LibraryStorageAudit, (string softUserId, IStoryCatalog catalog, IOptions<StoryTimeOptions> options) =>
{
    var entries = catalog.Snapshot(softUserId);
    var narrativeMarkers = ResolveNarrativeMarkers(options.Value.Catalog.NarrativeLeakageMarkers);
    var narrativeTextMinWords = Math.Max(1, options.Value.Catalog.NarrativeTextMinWords);
    var semanticNarrativeTextMinWords = Math.Max(1, options.Value.Catalog.SemanticNarrativeTextMinWords);
    var audioPayloadPrefix = options.Value.Generation.DataUris.AudioPayloadPrefix;
    var containsNarrativeText = entries.Any(entry =>
        ContainsNarrativeTextLeakage(entry, narrativeMarkers, narrativeTextMinWords, audioPayloadPrefix));
    var containsNarrativeAudioPayload = entries.Any(item =>
        !string.IsNullOrWhiteSpace(item.FullAudio));
    var containsSemanticNarrativeLeakage = entries.Any(entry =>
        ContainsSemanticNarrativeLeakage(entry, narrativeMarkers, semanticNarrativeTextMinWords, audioPayloadPrefix));

    return Results.Ok(new
    {
        entryCount = entries.Count,
        containsNarrativeText,
        containsNarrativeAudioPayload,
        containsSemanticNarrativeLeakage
    });
});

app.Run();

static string BuildLibraryTitle(GeneratedStory generated, StoryTimeOptions options)
{
    var shelfMode = string.Equals(generated.Mode, StoryModes.Series, StringComparison.OrdinalIgnoreCase)
        ? options.Generation.ModeLabels.Series
        : options.Generation.ModeLabels.OneShot;
    var episodeNumber = generated.StoryBible?.ArcEpisodeNumber ?? 1;
    var arc = generated.StoryBible?.ArcName;
    if (string.IsNullOrWhiteSpace(arc))
    {
        var template = options.Catalog.LibraryTitleWithoutArcTemplate;
        return template
            .Replace("{ModeLabel}", shelfMode, StringComparison.Ordinal)
            .Replace("{GeneratedAtHHmm}", generated.GeneratedAt.ToString("HHmm"), StringComparison.Ordinal)
            .Replace("{GeneratedAtIso}", generated.GeneratedAt.ToString("O"), StringComparison.Ordinal)
            .Replace("{EpisodeNumber}", episodeNumber.ToString(), StringComparison.Ordinal);
    }

    var withArcTemplate = options.Catalog.LibraryTitleWithArcTemplate;
    return withArcTemplate
        .Replace("{ModeLabel}", shelfMode, StringComparison.Ordinal)
        .Replace("{ArcName}", arc, StringComparison.Ordinal)
        .Replace("{EpisodeNumber}", episodeNumber.ToString(), StringComparison.Ordinal)
        .Replace("{GeneratedAtHHmm}", generated.GeneratedAt.ToString("HHmm"), StringComparison.Ordinal)
        .Replace("{GeneratedAtIso}", generated.GeneratedAt.ToString("O"), StringComparison.Ordinal);
}

static string BuildUpgradeMessage(StoryTimeOptions options, string upgradeTier)
{
    return options.Messages.UpgradeForLongerStories
        .Replace("{UpgradeTier}", upgradeTier, StringComparison.Ordinal);
}

static string[] ResolveNarrativeMarkers(IReadOnlyList<string> configuredMarkers)
{
    return configuredMarkers
        .Where(marker => !string.IsNullOrWhiteSpace(marker))
        .Select(marker => marker.Trim().ToLowerInvariant())
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}

static string[] NormalizeEntries(IReadOnlyList<string> values)
{
    return values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static bool ContainsNarrativeTextLeakage(
    StoryLibraryItem item,
    IReadOnlyList<string> narrativeMarkers,
    int minWords,
    string audioPayloadPrefix)
{
    return EnumerateInspectableText(item).Any(text => IsNarrativeText(text, minWords, narrativeMarkers, audioPayloadPrefix));
}

static bool ContainsSemanticNarrativeLeakage(
    StoryLibraryItem item,
    IReadOnlyList<string> narrativeMarkers,
    int minWords,
    string audioPayloadPrefix)
{
    return EnumerateInspectableText(item).Any(text => IsNarrativeText(text, minWords, narrativeMarkers, audioPayloadPrefix));
}

static IEnumerable<string> EnumerateInspectableText(StoryLibraryItem item)
{
    if (!string.IsNullOrWhiteSpace(item.Title))
    {
        yield return item.Title;
    }

    if (!string.IsNullOrWhiteSpace(item.FullAudio))
    {
        yield return item.FullAudio;
    }
}

static bool IsNarrativeText(string text, int minWords, IReadOnlyList<string> narrativeMarkers, string audioPayloadPrefix)
{
    if (!string.IsNullOrWhiteSpace(audioPayloadPrefix) &&
        text.StartsWith(audioPayloadPrefix, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var normalized = text.Trim().ToLowerInvariant();
    if (normalized.Length == 0)
    {
        return false;
    }

    if (narrativeMarkers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal)))
    {
        return true;
    }

    var wordCount = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    return wordCount >= minWords;
}

static bool IsWebhookAuthorized(HttpRequest request, string configuredSecret)
{
    if (string.IsNullOrWhiteSpace(configuredSecret))
    {
        return false;
    }

    if (!request.Headers.TryGetValue("X-StoryTime-Webhook-Secret", out var providedValues))
    {
        return false;
    }

    var providedSecret = providedValues.ToString();
    if (string.IsNullOrWhiteSpace(providedSecret))
    {
        return false;
    }

    var expectedBytes = Encoding.UTF8.GetBytes(configuredSecret);
    var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
    return expectedBytes.Length == providedBytes.Length &&
        CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
}

static string ResolveGateToken(HttpRequest request)
{
    if (request.Headers.TryGetValue("X-StoryTime-Gate-Token", out var headerValues))
    {
        var headerToken = headerValues.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(headerToken))
        {
            return headerToken;
        }
    }

    return string.Empty;
}

public partial class Program;
