using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using StoryTime.Api.Domain;
using StoryTime.Api.Services;
using System.Net.Http;

namespace StoryTime.Api.Tests.Unit;

public sealed class FileSystemStoryCatalogTests
{
    [Fact]
    public void AddAndApprove_PersistMetadataOnlyAcrossInstances()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "storytime-catalog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var catalogPath = Path.Combine(tempRoot, "catalog.json");

        try
        {
            var options = StoryTimeOptionsFactory.Create();
            options.Catalog.Provider = "FileSystem";
            options.Catalog.FilePath = catalogPath;
            var wrappedOptions = Options.Create(options);
            var mediaAssetService = new ProceduralMediaAssetService(
                wrappedOptions,
                NullLogger<ProceduralMediaAssetService>.Instance,
                new TestHttpClientFactory());

            var catalog = new FileSystemStoryCatalog(wrappedOptions, mediaAssetService);
            catalog.Add(
                "user-persist",
                new StoryLibraryItem
                {
                    StoryId = "story-1",
                    Title = "Story one",
                    Mode = "one-shot",
                    IsFavorite = false,
                    FullAudioReady = false,
                    FullAudio = "data:audio/wav;base64,ABC",
                    CreatedAt = DateTimeOffset.UtcNow
                });

            var approved = catalog.SetApproval("story-1");
            Assert.NotNull(approved);
            Assert.True(approved!.FullAudioReady);
            Assert.StartsWith("data:audio/wav;base64,", approved.FullAudio);

            var reloaded = new FileSystemStoryCatalog(wrappedOptions, mediaAssetService);
            var recent = reloaded.GetRecent("user-persist");

            Assert.Single(recent);
            Assert.Equal("story-1", recent[0].StoryId);
            Assert.True(recent[0].FullAudioReady);
            Assert.Null(recent[0].FullAudio);

            var persisted = File.ReadAllText(catalogPath);
            Assert.DoesNotContain("data:audio/wav;base64", persisted, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
