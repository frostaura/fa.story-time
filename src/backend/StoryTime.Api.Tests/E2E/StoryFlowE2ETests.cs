using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StoryTime.Api.Contracts;

namespace StoryTime.Api.Tests.E2E;

public sealed class StoryFlowE2ETests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

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

    private sealed record StorageAuditResponse(
        int EntryCount,
        bool ContainsNarrativeText,
        bool ContainsNarrativeAudioPayload,
        bool ContainsSemanticNarrativeLeakage);
}
