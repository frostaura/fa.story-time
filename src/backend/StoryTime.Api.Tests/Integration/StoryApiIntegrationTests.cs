using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Binary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StoryTime.Api.Contracts;
using StoryTime.Api.Domain;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Integration;

public sealed class StoryApiIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder =>
            builder.UseSetting("StoryTime:Generation:AiOrchestration:Enabled", "false"))
        .CreateClient();

    [Fact]
    public async Task HomeStatus_IsQuickGenerateFirst()
    {
        var response = await _client.GetFromJsonAsync<HomeStatusResponse>("/api/home/status");

        Assert.NotNull(response);
        Assert.True(response!.QuickGenerateVisible);
        Assert.True(response.DurationSliderVisible);
        Assert.Equal(5, response.DurationMinMinutes);
        Assert.Equal(15, response.DurationMaxMinutes);
        Assert.Equal("Dreamer", response.DefaultChildName);
        Assert.True(response.ParentControlsEnabled);
        Assert.Equal("Trial", response.DefaultTier);
        Assert.Equal("a gentle friend", response.OneShotDefaults.CompanionName);
        Assert.Equal("moonlit meadow paths", response.OneShotDefaults.Setting);
    }

    [Fact]
    public async Task CorsPolicy_AllowsConfiguredFrontendOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/home/status");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await _client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Contains("http://localhost:5173", values);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods));
        Assert.Contains("GET", string.Join(",", methods), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CorsPolicy_DoesNotAllowUnknownOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/home/status");
        request.Headers.Add("Origin", "https://malicious.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await _client.SendAsync(request);

        Assert.False(response.Headers.TryGetValues("Access-Control-Allow-Origin", out _));
    }

    [Fact]
    public async Task StoryFavorite_DeleteVerbIsRejected()
    {
        using var response = await _client.DeleteAsync("/api/stories/story-id/favorite");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task GenerateThenApprove_SucceedsAndTracksFavoriteInLibrary()
    {
        var request = new GenerateStoryRequest("user-library", "Ari", "series", 6, null, null, true, ReducedMotion: true);
        using var generateResult = await _client.PostAsJsonAsync("/api/stories/generate", request);

        generateResult.EnsureSuccessStatusCode();
        var generated = await generateResult.Content.ReadFromJsonAsync<GenerateStoryResponse>();

        Assert.NotNull(generated);
        Assert.True(generated!.ApprovalRequired);
        Assert.Equal("series", generated.Mode);
        Assert.InRange(generated.PosterLayers.Count, 3, 5);
        Assert.True(generated.ReducedMotion);
        Assert.NotNull(generated.StoryBible);
        Assert.NotEmpty(generated.Scenes);

        using var favoriteResult = await _client.PutAsJsonAsync($"/api/stories/{generated.StoryId}/favorite", new SetFavoriteRequest(true));
        favoriteResult.EnsureSuccessStatusCode();

        using var approveResult = await _client.PostAsync($"/api/stories/{generated.StoryId}/approve", content: null);
        approveResult.EnsureSuccessStatusCode();
        var approval = await approveResult.Content.ReadFromJsonAsync<StoryApprovalResponse>();
        Assert.NotNull(approval);
        Assert.True(approval!.FullAudioReady);
        Assert.StartsWith("data:audio/wav;base64,", approval.FullAudio);

        var library = await _client.GetFromJsonAsync<LibraryResponse>("/api/library/user-library?kidMode=true");

        Assert.NotNull(library);
        Assert.True(library!.KidModeEnabled);
        Assert.NotEmpty(library.Recent);
        Assert.NotEmpty(library.Favorites);
        Assert.All(library.Recent, item => Assert.DoesNotContain("Ari", item.Title, StringComparison.OrdinalIgnoreCase));
        Assert.All(library.Recent, item => Assert.Null(item.FullAudio));
        Assert.All(library.Favorites, item => Assert.Null(item.FullAudio));
    }

    [Fact]
    public async Task KidMode_EnforcesServerSideShelfLimits()
    {
        const string userId = "user-kid-limits";
        for (var index = 0; index < 10; index++)
        {
            using var webhook = await _client.PostAsJsonAsync(
                "/api/subscription/webhook",
                new SubscriptionWebhookRequest(userId, "Premium", true));
            webhook.EnsureSuccessStatusCode();

            using var generated = await _client.PostAsJsonAsync(
                "/api/stories/generate",
                new GenerateStoryRequest(userId, "Ivy", "one-shot", 6, null, null, Favorite: true));
            generated.EnsureSuccessStatusCode();
        }

        var nonKid = await _client.GetFromJsonAsync<LibraryResponse>($"/api/library/{userId}?kidMode=false");
        var kid = await _client.GetFromJsonAsync<LibraryResponse>($"/api/library/{userId}?kidMode=true");

        Assert.NotNull(nonKid);
        Assert.NotNull(kid);
        Assert.True(nonKid!.Recent.Count > kid!.Recent.Count);
        Assert.True(nonKid.Favorites.Count > kid.Favorites.Count);
        Assert.True(kid.Recent.Count <= 8);
        Assert.True(kid.Favorites.Count <= 8);
    }

    [Fact]
    public async Task ParentSettings_RequireGateAndAllowControlledUpdates()
    {
        var (credentialId, publicKey, privateKey) = CreateCredentialPair();
        using var registerResponse = await _client.PostAsJsonAsync(
            "/api/parent/parent-user/gate/register",
            new ParentCredentialRegisterRequest(credentialId, publicKey));
        registerResponse.EnsureSuccessStatusCode();

        using var challengeResponse = await _client.PostAsync("/api/parent/parent-user/gate/challenge", null);
        challengeResponse.EnsureSuccessStatusCode();

        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ParentGateChallengeResponse>();
        Assert.NotNull(challenge);

        using var verifyResponse = await _client.PostAsJsonAsync(
            "/api/parent/parent-user/gate/verify",
            new ParentGateVerifyRequest(
                challenge!.ChallengeId,
                BuildAssertion(challenge.Challenge, credentialId, privateKey)));
        verifyResponse.EnsureSuccessStatusCode();

        var gate = await verifyResponse.Content.ReadFromJsonAsync<ParentGateVerifyResponse>();
        Assert.NotNull(gate);

        var settings = await _client.GetFromJsonAsync<ParentSettingsResponse>($"/api/parent/parent-user/settings?gateToken={gate!.GateToken}");
        Assert.NotNull(settings);

        using var updateResponse = await _client.PutAsJsonAsync(
            "/api/parent/parent-user/settings",
            new ParentSettingsUpdateRequest(gate.GateToken, NotificationsEnabled: true, AnalyticsEnabled: true));
        updateResponse.EnsureSuccessStatusCode();

        var updated = await updateResponse.Content.ReadFromJsonAsync<ParentSettingsResponse>();
        Assert.NotNull(updated);
        Assert.True(updated!.NotificationsEnabled);
        Assert.True(updated.AnalyticsEnabled);
    }

    [Fact]
    public async Task CheckoutFlow_RequiresParentGate_AndUpgradesTier()
    {
        const string userId = "user-checkout-flow";
        var gateToken = await CreateGateTokenAsync(userId);

        using var createCheckout = await _client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/session",
            new SubscriptionCheckoutSessionRequest(gateToken, "Premium"));
        createCheckout.EnsureSuccessStatusCode();
        var checkoutSession = await createCheckout.Content.ReadFromJsonAsync<SubscriptionCheckoutSessionResponse>();
        Assert.NotNull(checkoutSession);
        Assert.Equal("Premium", checkoutSession!.UpgradeTier);

        using var completeCheckout = await _client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/complete",
            new SubscriptionCheckoutCompleteRequest(gateToken, checkoutSession.SessionId));
        completeCheckout.EnsureSuccessStatusCode();
        var completion = await completeCheckout.Content.ReadFromJsonAsync<SubscriptionCheckoutCompleteResponse>();
        Assert.NotNull(completion);
        Assert.Equal("Premium", completion!.CurrentTier);

        using var upgradedGeneration = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest(userId, "Kai", "series", 15, null, null, false));
        upgradedGeneration.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CheckoutFlow_RejectsMissingOrCrossUserGateAuthorization()
    {
        const string userId = "user-checkout-auth";
        var validGateToken = await CreateGateTokenAsync("gate-owner");

        using var missingGate = await _client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/session",
            new SubscriptionCheckoutSessionRequest("missing-gate-token", "Premium"));
        Assert.Equal(HttpStatusCode.Unauthorized, missingGate.StatusCode);

        using var crossUserGate = await _client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/session",
            new SubscriptionCheckoutSessionRequest(validGateToken, "Premium"));
        Assert.Equal(HttpStatusCode.Unauthorized, crossUserGate.StatusCode);
    }

    [Fact]
    public async Task CheckoutComplete_ReturnsBadRequestForUnknownSession()
    {
        const string userId = "user-checkout-invalid-session";
        var gateToken = await CreateGateTokenAsync(userId);

        using var completeCheckout = await _client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/complete",
            new SubscriptionCheckoutCompleteRequest(gateToken, "missing-session-id"));
        Assert.Equal(HttpStatusCode.BadRequest, completeCheckout.StatusCode);
    }

    [Fact]
    public async Task CheckoutComplete_ReturnsBadRequestForExpiredSession()
    {
        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("StoryTime:Generation:AiOrchestration:Enabled", "false");
            builder.UseSetting("StoryTime:Checkout:Provider:Mode", "External");
            builder.UseSetting("StoryTime:Checkout:Provider:LocalFallbackEnabled", "false");
            builder.UseSetting("StoryTime:Checkout:Provider:Endpoint", "https://payments.storytime.test/storytime/checkout");
            builder.ConfigureServices(services =>
            {
                services.AddHttpClient(nameof(SubscriptionPolicyService))
                    .ConfigurePrimaryHttpMessageHandler(() => new ExpiredCheckoutProviderHandler());
            });
        }).CreateClient();

        const string userId = "user-checkout-expired-session";
        var gateToken = await CreateGateTokenAsync(client, userId);

        using var createCheckout = await client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/session",
            new SubscriptionCheckoutSessionRequest(gateToken, "Premium"));
        createCheckout.EnsureSuccessStatusCode();
        var checkoutSession = await createCheckout.Content.ReadFromJsonAsync<SubscriptionCheckoutSessionResponse>();
        Assert.NotNull(checkoutSession);

        using var completeCheckout = await client.PostAsJsonAsync(
            $"/api/subscription/{userId}/checkout/complete",
            new SubscriptionCheckoutCompleteRequest(gateToken, checkoutSession!.SessionId));
        Assert.Equal(HttpStatusCode.BadRequest, completeCheckout.StatusCode);
    }

    [Fact]
    public async Task GenerateApproveLibraryFlow_RemainsStableAcrossRepeatedRuns()
    {
        const string userId = "user-repeated-readiness";
        for (var iteration = 0; iteration < 3; iteration++)
        {
            using var webhook = await _client.PostAsJsonAsync(
                "/api/subscription/webhook",
                new SubscriptionWebhookRequest(userId, "Premium", true));
            webhook.EnsureSuccessStatusCode();

            using var generatedResponse = await _client.PostAsJsonAsync(
                "/api/stories/generate",
                new GenerateStoryRequest(userId, "Ari", "series", 15, null, null, false));
            generatedResponse.EnsureSuccessStatusCode();
            var generated = await generatedResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();
            Assert.NotNull(generated);

            using var approve = await _client.PostAsync($"/api/stories/{generated!.StoryId}/approve", content: null);
            approve.EnsureSuccessStatusCode();

            var library = await _client.GetFromJsonAsync<LibraryResponse>($"/api/library/{userId}?kidMode=false");
            Assert.NotNull(library);
            Assert.Contains(library!.Recent, item => item.StoryId == generated.StoryId);
        }
    }

    [Fact]
    public async Task TrialTier_RejectsLongStoriesAndCooldownRepeats()
    {
        using var longStoryAttempt = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest("user-trial", "Kai", "one-shot", 15, null, null, false));

        Assert.Equal(HttpStatusCode.PaymentRequired, longStoryAttempt.StatusCode);
        var rejectionJson = await longStoryAttempt.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(rejectionJson.TryGetProperty("paywall", out var paywallJson));
        Assert.True(paywallJson.TryGetProperty("message", out var paywallMessageJson));
        Assert.Equal("Upgrade to Premium for longer bedtime stories.", paywallMessageJson.GetString());

        using var firstOk = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest("user-trial-cooldown", "Kai", "series", 5, null, null, false));

        firstOk.EnsureSuccessStatusCode();

        using var cooldownAttempt = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest("user-trial-cooldown", "Kai", "series", 5, null, null, false));

        Assert.Equal(HttpStatusCode.TooManyRequests, cooldownAttempt.StatusCode);
    }

    [Fact]
    public async Task StoriesGenerate_ReturnsBadRequestForMalformedJsonPayload()
    {
        using var malformedRequest = new StringContent(
            "{\"softUserId\":\"user-malformed\"",
            Encoding.UTF8,
            "application/json");
        using var response = await _client.PostAsync("/api/stories/generate", malformedRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ParentGateVerify_ReturnsBadRequestForMalformedJsonPayload()
    {
        using var malformedRequest = new StringContent(
            "{\"challengeId\":\"missing-quote}",
            Encoding.UTF8,
            "application/json");
        using var response = await _client.PostAsync("/api/parent/parent-malformed/gate/verify", malformedRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubscriptionPaywall_ReturnsPaywallInfoForTrialUser()
    {
        const string userId = "user-paywall-trial";

        var paywall = await _client.GetFromJsonAsync<SubscriptionPaywallResponse>($"/api/subscription/{userId}/paywall");

        Assert.NotNull(paywall);
        Assert.Equal("Trial", paywall!.CurrentTier);
        Assert.Equal("Premium", paywall.UpgradeTier);
        Assert.Equal(10, paywall.MaxDurationMinutes);
        Assert.Equal("/subscribe", paywall.UpgradeUrl);
        Assert.Contains("Premium", paywall.Message);
    }

    [Fact]
    public async Task SubscriptionPaywall_ReflectsUpgradedTier()
    {
        const string userId = "user-paywall-upgraded";
        using var webhook = await _client.PostAsJsonAsync(
            "/api/subscription/webhook",
            new SubscriptionWebhookRequest(userId, "Premium", true));
        webhook.EnsureSuccessStatusCode();

        var paywall = await _client.GetFromJsonAsync<SubscriptionPaywallResponse>($"/api/subscription/{userId}/paywall");

        Assert.NotNull(paywall);
        Assert.Equal("Premium", paywall!.CurrentTier);
        Assert.Equal(15, paywall.MaxDurationMinutes);
    }

    [Fact]
    public async Task NonApprovalFlow_DoesNotPersistNarrativeAudioOrLeakage()
    {
        const string userId = "user-non-approval";

        using var generatedResponse = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest(userId, "Ari", "one-shot", 6, null, false, false));

        generatedResponse.EnsureSuccessStatusCode();
        var generated = await generatedResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();

        Assert.NotNull(generated);
        Assert.False(generated!.ApprovalRequired);
        Assert.True(generated.FullAudioReady);
        Assert.StartsWith("data:audio/wav;base64,", generated.FullAudio);

        var library = await _client.GetFromJsonAsync<LibraryResponse>($"/api/library/{userId}?kidMode=false");
        Assert.NotNull(library);
        Assert.NotEmpty(library!.Recent);
        Assert.All(library.Recent, item => Assert.Null(item.FullAudio));

        var storageAudit = await _client.GetFromJsonAsync<StorageAuditResponse>($"/api/library/{userId}/storage-audit");
        Assert.NotNull(storageAudit);
        Assert.False(storageAudit!.ContainsNarrativeText);
        Assert.False(storageAudit.ContainsNarrativeAudioPayload);
        Assert.False(storageAudit.ContainsSemanticNarrativeLeakage);
    }

    private static ParentGateAssertion BuildAssertion(string challenge, string credentialId, byte[] privateKey)
    {
        var clientDataRaw = JsonSerializer.Serialize(new
        {
            type = ParentGateAssertionTypes.WebAuthnGet,
            challenge,
            origin = "http://localhost"
        });
        var clientDataBytes = Encoding.UTF8.GetBytes(clientDataRaw);
        var authenticatorDataBytes = BuildAuthenticatorData("localhost", 1);
        var clientDataHash = SHA256.HashData(clientDataBytes);

        var signedPayload = new byte[authenticatorDataBytes.Length + clientDataHash.Length];
        Buffer.BlockCopy(authenticatorDataBytes, 0, signedPayload, 0, authenticatorDataBytes.Length);
        Buffer.BlockCopy(clientDataHash, 0, signedPayload, authenticatorDataBytes.Length, clientDataHash.Length);

        using var key = ECDsa.Create();
        key.ImportECPrivateKey(privateKey, out _);
        var signature = key.SignData(signedPayload, HashAlgorithmName.SHA256);

        return new ParentGateAssertion(
            CredentialId: credentialId,
            ClientDataJson: Convert.ToBase64String(clientDataBytes),
            AuthenticatorData: Convert.ToBase64String(authenticatorDataBytes),
            Signature: Convert.ToBase64String(signature),
            Type: ParentGateAssertionTypes.WebAuthnGet);
    }

    private static byte[] BuildAuthenticatorData(string rpId, uint signCount)
    {
        var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
        var authenticatorData = new byte[37];
        Buffer.BlockCopy(rpIdHash, 0, authenticatorData, 0, rpIdHash.Length);
        authenticatorData[32] = 0x01;
        BinaryPrimitives.WriteUInt32BigEndian(authenticatorData.AsSpan(33, 4), signCount);
        return authenticatorData;
    }

    private static (string CredentialId, string PublicKey, byte[] PrivateKey) CreateCredentialPair()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = Guid.NewGuid().ToString("N");
        var publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        var privateKey = key.ExportECPrivateKey();
        return (credentialId, publicKey, privateKey);
    }

    private Task<string> CreateGateTokenAsync(string userId) => CreateGateTokenAsync(_client, userId);

    private async Task<string> CreateGateTokenAsync(HttpClient client, string userId)
    {
        var (credentialId, publicKey, privateKey) = CreateCredentialPair();

        using var registerResponse = await client.PostAsJsonAsync(
            $"/api/parent/{userId}/gate/register",
            new ParentCredentialRegisterRequest(credentialId, publicKey));
        registerResponse.EnsureSuccessStatusCode();

        using var challengeResponse = await client.PostAsync($"/api/parent/{userId}/gate/challenge", null);
        challengeResponse.EnsureSuccessStatusCode();
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ParentGateChallengeResponse>();
        Assert.NotNull(challenge);

        using var verifyResponse = await client.PostAsJsonAsync(
            $"/api/parent/{userId}/gate/verify",
            new ParentGateVerifyRequest(challenge!.ChallengeId, BuildAssertion(challenge.Challenge, credentialId, privateKey)));
        verifyResponse.EnsureSuccessStatusCode();
        var gate = await verifyResponse.Content.ReadFromJsonAsync<ParentGateVerifyResponse>();
        Assert.NotNull(gate);
        return gate!.GateToken;
    }

    private sealed class ExpiredCheckoutProviderHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.Contains("/storytime/checkout/session", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"sessionId":"expired-session","checkoutUrl":"https://payments.storytime.dev/session/expired-session","expiresAt":"2000-01-01T00:00:00Z","upgradeTier":"Premium"}""",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            if (path.Contains("/storytime/checkout/complete", StringComparison.Ordinal))
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

    private sealed record StorageAuditResponse(
        int EntryCount,
        bool ContainsNarrativeText,
        bool ContainsNarrativeAudioPayload,
        bool ContainsSemanticNarrativeLeakage);
}
