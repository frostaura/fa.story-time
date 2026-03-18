using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Unit;

public sealed class ProceduralMediaAssetServiceTests
{
    [Fact]
    public void BuildPosterLayers_UsesProceduralFallbackWithinBudget_WhenLiveProviderFails()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterModelProvider.Enabled = true;
        options.Generation.PosterModelProvider.LocalFallbackEnabled = true;
        options.Generation.PosterModelProvider.Endpoint = "https://poster.storytime.test/generate";

        var service = CreateService(options, new StatusCodeHandler(HttpStatusCode.BadGateway));

        var layers = service.BuildPosterLayers("poster-fallback-seed", reducedMotion: false);

        Assert.Equal(4, layers.Count);
        Assert.Collection(
            layers,
            layer => Assert.Equal("BACKGROUND", layer.Role),
            layer => Assert.Equal("MIDGROUND_1", layer.Role),
            layer => Assert.Equal("FOREGROUND", layer.Role),
            layer => Assert.Equal("PARTICLES", layer.Role));
        Assert.All(layers, layer => Assert.StartsWith(options.Generation.DataUris.PosterSvgBase64Prefix, layer.DataUri, StringComparison.Ordinal));
        Assert.All(layers, layer => Assert.True(layer.SpeedMultiplier > 0));
    }

    [Fact]
    public void BuildPosterLayers_ThrowsExplicitly_WhenLiveProviderFailsWithoutFallback()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.PosterModelProvider.Enabled = true;
        options.Generation.PosterModelProvider.LocalFallbackEnabled = false;
        options.Generation.PosterModelProvider.Endpoint = "https://poster.storytime.test/generate";

        var service = CreateService(options, new StatusCodeHandler(HttpStatusCode.BadGateway));

        var exception = Assert.Throws<InvalidOperationException>(() => service.BuildPosterLayers("poster-provider-error", reducedMotion: false));

        Assert.Contains("failed with status 502", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAudioDataUri_ThrowsExplicitly_WhenNarrationProviderReturnsNonAudioPayloadWithoutFallback()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Generation.NarrationProvider.Enabled = true;
        options.Generation.NarrationProvider.LocalFallbackEnabled = false;
        options.Generation.NarrationProvider.Endpoint = "https://narration.storytime.test/audio";

        var service = CreateService(
            options,
            new RawResponseHandler(
                HttpStatusCode.OK,
                """{"dataUri":"data:text/plain;base64,SGVsbG8="}"""));

        var exception = Assert.Throws<InvalidOperationException>(() => service.BuildAudioDataUri("story-audio-1", 8, 0.06));

        Assert.Contains("non-audio payload", exception.Message, StringComparison.Ordinal);
    }

    private static ProceduralMediaAssetService CreateService(StoryTimeOptions options, HttpMessageHandler handler)
    {
        return new ProceduralMediaAssetService(
            Options.Create(options),
            NullLogger<ProceduralMediaAssetService>.Instance,
            new TestHttpClientFactory(handler));
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class StatusCodeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class RawResponseHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }
}
