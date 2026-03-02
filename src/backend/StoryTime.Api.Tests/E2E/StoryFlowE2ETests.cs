using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using StoryTime.Api.Contracts;

namespace StoryTime.Api.Tests.E2E;

public sealed class StoryFlowE2ETests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder =>
            builder.UseSetting("StoryTime:Generation:AiOrchestration:Enabled", "false"))
        .CreateClient();

    [Fact]
    public async Task PremiumSeriesFlow_IsCoherentAndStorageSafe()
    {
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
            new GenerateStoryRequest("user-premium", "Lena", "series", 15, firstStory.SeriesId, null, false));

        continuationResponse.EnsureSuccessStatusCode();
        var continuation = await continuationResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();

        Assert.NotNull(continuation);
        Assert.Equal(firstStory.SeriesId, continuation!.SeriesId);
        Assert.StartsWith("Previously:", continuation.Recap);
        Assert.NotNull(continuation.StoryBible);
        Assert.True(continuation.StoryBible!.ArcEpisodeNumber >= 2);
        Assert.False(string.IsNullOrWhiteSpace(continuation.StoryBible.AudioAnchorMetadata.ThemeTrackId));

        using var approve = await _client.PostAsync($"/api/stories/{continuation.StoryId}/approve", content: null);
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

        var library = await _client.GetFromJsonAsync<LibraryResponse>("/api/library/user-one-shot?kidMode=false");
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
    public async Task SeriesPersistence_StripsNarrativeFieldsFromPersistedStoryBible()
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

        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("StoryTime:Generation:AiOrchestration:Enabled", "false");
            builder.UseSetting("StoryTime:Generation:PersistSeriesStoryBible", "true");
            builder.UseSetting("StoryTime:Generation:PersistContinuityFacts", "true");
            builder.UseSetting("StoryTime:Generation:StoryBibleFilePath", storyBibleFilePath);
        }).CreateClient();

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
                new GenerateStoryRequest(userId, "Lena", "series", 15, firstStory.SeriesId, null, false));
            secondResponse.EnsureSuccessStatusCode();
            var continuation = await secondResponse.Content.ReadFromJsonAsync<GenerateStoryResponse>();
            Assert.NotNull(continuation);
            Assert.NotNull(continuation!.StoryBible);

            Assert.True(File.Exists(storyBibleFilePath));
            var persisted = await File.ReadAllTextAsync(storyBibleFilePath);
            Assert.DoesNotContain(continuation.StoryBible!.ArcObjective, persisted, StringComparison.Ordinal);
            Assert.DoesNotContain(continuation.StoryBible.PreviousEpisodeSummary, persisted, StringComparison.Ordinal);

            using var persistedJson = JsonDocument.Parse(persisted);
            var persistedEntry = persistedJson.RootElement[0];
            Assert.Equal("Episode 2 completed.", persistedEntry.GetProperty("LastEpisodeSummary").GetString());
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
}
