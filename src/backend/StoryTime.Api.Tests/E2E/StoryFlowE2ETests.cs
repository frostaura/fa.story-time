using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using StoryTime.Api.Contracts;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Tests.E2E;

public sealed class StoryFlowE2ETests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestWebhookSecret = "storytime-local-webhook-secret";
    private readonly HttpClient _client = CreateConfiguredClient(factory);

    [Fact]
    public async Task PremiumSeriesFlow_IsCoherentAndStorageSafe()
    {
        var gateToken = await CreateGateTokenAsync("user-premium");
        using var webhook = await _client.PostAsJsonAsync(
            "/api/subscription/webhook",
            new SubscriptionWebhookRequest("user-premium", "Premium", true));

        webhook.EnsureSuccessStatusCode();

        using var firstResponse = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest("user-premium", "Lena", "series", 15, null, null, false));

        firstResponse.EnsureSuccessStatusCode();
        var firstStory = await firstResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();

        Assert.NotNull(firstStory);
        Assert.False(string.IsNullOrWhiteSpace(firstStory!.SeriesId));

        await _client.PostAsJsonAsync(
            "/api/subscription/webhook",
            new SubscriptionWebhookRequest("user-premium", "Premium", true));

        using var continuationResponse = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest("user-premium", "Lena", "series", 15, firstStory.SeriesId, null, false, StoryBible: firstStory.StoryBible));

        continuationResponse.EnsureSuccessStatusCode();
        var continuation = await continuationResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();

        Assert.NotNull(continuation);
        Assert.Equal(firstStory.SeriesId, continuation!.SeriesId);
        Assert.StartsWith("Previously:", continuation.Recap);
        Assert.NotNull(continuation.StoryBible);
        Assert.True(continuation.StoryBible!.ArcEpisodeNumber >= 2);
        Assert.False(string.IsNullOrWhiteSpace(continuation.StoryBible.AudioAnchorMetadata.ThemeTrackId));

        using var approve = await _client.PostAsJsonAsync(
            $"/api/stories/{continuation.StoryId}/approve",
            new StoryApprovalRequest("user-premium", gateToken));
        approve.EnsureSuccessStatusCode();
        var approval = await approve.Content.ReadFromJsonAsync<StoryApprovalResponse>();
        Assert.NotNull(approval);
        Assert.True(approval!.FullAudioReady);
        Assert.StartsWith("data:audio/wav;base64,", approval.FullAudio);

        var storageAudit = await _client.GetFromJsonAsync<StorageAuditResponse>("/api/library/user-premium/storage-audit");

        Assert.NotNull(storageAudit);
        Assert.False(storageAudit!.ContainsNarrativeText);
        Assert.False(storageAudit.ContainsNarrativeAudioPayload);
        Assert.False(storageAudit.ContainsSemanticNarrativeLeakage);
    }

    [Fact]
    public async Task OneShotWithoutApproval_StoresMetadataButNoNarrativePayload()
    {
        using var generatedResponse = await _client.PostAsJsonAsync(
            "/api/stories/generate",
            new GenerateStoryRequest("user-one-shot", "Lena", "one-shot", 6, null, false, false));

        generatedResponse.EnsureSuccessStatusCode();
        var generated = await generatedResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();
        Assert.NotNull(generated);
        Assert.False(generated!.ApprovalRequired);
        Assert.True(generated.FullAudioReady);
        Assert.StartsWith("data:audio/wav;base64,", generated.FullAudio);

        var library = await _client.GetFromJsonAsync<LibraryResponse>("/api/library/user-one-shot");
        Assert.NotNull(library);
        Assert.NotEmpty(library!.Recent);
        Assert.All(library.Recent, item => Assert.Null(item.FullAudio));

        var storageAudit = await _client.GetFromJsonAsync<StorageAuditResponse>("/api/library/user-one-shot/storage-audit");
        Assert.NotNull(storageAudit);
        Assert.False(storageAudit!.ContainsNarrativeText);
        Assert.False(storageAudit.ContainsNarrativeAudioPayload);
        Assert.False(storageAudit.ContainsSemanticNarrativeLeakage);
    }

    [Fact]
    public async Task SeriesContinuation_UsesClientStoryBibleSnapshot_WithoutPersistingServerFiles()
    {
        var storyBibleFilePath = Path.Combine(
            Path.GetTempPath(),
            "storytime-e2e-story-bibles",
            $"{Guid.NewGuid():N}.json");
        var storyBibleDirectory = Path.GetDirectoryName(storyBibleFilePath);
        if (!string.IsNullOrWhiteSpace(storyBibleDirectory))
        {
            Directory.CreateDirectory(storyBibleDirectory);
        }

        using var client = CreateConfiguredClient(factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("StoryTime:Generation:ContinuityFactRetentionLimit", "30");
        }));

        try
        {
            const string userId = "user-privacy-safe-series";
            using var webhook = await client.PostAsJsonAsync(
                "/api/subscription/webhook",
                new SubscriptionWebhookRequest(userId, "Premium", true));
            webhook.EnsureSuccessStatusCode();

            using var firstResponse = await client.PostAsJsonAsync(
                "/api/stories/generate",
                new GenerateStoryRequest(userId, "Lena", "series", 15, null, null, false));
            firstResponse.EnsureSuccessStatusCode();
            var firstStory = await firstResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();
            Assert.NotNull(firstStory);
            Assert.False(string.IsNullOrWhiteSpace(firstStory!.SeriesId));

            using var continuationWebhook = await client.PostAsJsonAsync(
                "/api/subscription/webhook",
                new SubscriptionWebhookRequest(userId, "Premium", true));
            continuationWebhook.EnsureSuccessStatusCode();

            using var secondResponse = await client.PostAsJsonAsync(
                "/api/stories/generate",
                new GenerateStoryRequest(userId, "Lena", "series", 15, firstStory.SeriesId, null, false, StoryBible: firstStory.StoryBible));
            secondResponse.EnsureSuccessStatusCode();
            var continuation = await secondResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();
            Assert.NotNull(continuation);
            Assert.NotNull(continuation!.StoryBible);

            Assert.StartsWith("Previously:", continuation.Recap);
            Assert.Equal(continuation.SeriesId, continuation.StoryBible.SeriesId);
            Assert.False(File.Exists(storyBibleFilePath));
        }
        finally
        {
            if (File.Exists(storyBibleFilePath))
            {
                File.Delete(storyBibleFilePath);
            }
        }
    }

    private sealed record StorageAuditResponse(
        int EntryCount,
        bool ContainsNarrativeText,
        bool ContainsNarrativeAudioPayload,
        bool ContainsSemanticNarrativeLeakage);

    private static HttpClient CreateConfiguredClient(WebApplicationFactory<Program> factory)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "storytime-e2e-tests", Guid.NewGuid().ToString("N"));
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("StoryTime:Generation:AiOrchestration:Enabled", "false");
                builder.UseSetting("StoryTime:Generation:AiOrchestration:LocalFallbackEnabled", "false");
                builder.UseSetting("StoryTime:Checkout:WebhookSharedSecret", TestWebhookSecret);
                builder.UseSetting("StoryTime:ParentGate:RequireAssertion", "false");
                builder.UseSetting("StoryTime:ParentGate:RequireChallengeBoundAssertion", "false");
                builder.UseSetting("StoryTime:ParentGate:RequireRegisteredCredential", "false");
                builder.UseSetting("StoryTime:Catalog:Provider", "InMemory");
                builder.UseSetting("StoryTime:Catalog:FilePath", Path.Combine(tempRoot, "catalog.json"));
                builder.UseSetting("StoryTime:ParentGate:StateFilePath", Path.Combine(tempRoot, "parent-state.json"));
                builder.UseSetting("StoryTime:Checkout:StateFilePath", Path.Combine(tempRoot, "subscription-state.json"));
            })
            .CreateClient();
        AddWebhookSecret(client);
        return client;
    }

    private static void AddWebhookSecret(HttpClient client)
    {
        if (!client.DefaultRequestHeaders.Contains("X-StoryTime-Webhook-Secret"))
        {
            client.DefaultRequestHeaders.Add("X-StoryTime-Webhook-Secret", TestWebhookSecret);
        }
    }

    private async Task<string> CreateGateTokenAsync(string userId)
    {
        using var challengeResponse = await _client.PostAsync($"/api/parent/{userId}/gate/challenge", null);
        challengeResponse.EnsureSuccessStatusCode();
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<ParentGateChallengeResponse>();
        Assert.NotNull(challenge);

        using var verifyResponse = await _client.PostAsJsonAsync(
            $"/api/parent/{userId}/gate/verify",
            new ParentGateVerifyRequest(
                challenge!.ChallengeId,
                new ParentGateAssertion(string.Empty, string.Empty, string.Empty, string.Empty, ParentGateAssertionTypes.WebAuthnGet)));
        verifyResponse.EnsureSuccessStatusCode();
        var gate = await verifyResponse.Content.ReadFromJsonAsync<ParentGateVerifyResponse>();
        Assert.NotNull(gate);
        return gate!.GateToken;
    }
}
