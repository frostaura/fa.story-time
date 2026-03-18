using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StoryTime.Api;
using StoryTime.Api.Domain;
using StoryTime.Api.Services;

namespace StoryTime.Api.Tests.Unit;

public sealed class InMemoryStoryCatalogTests
{
    private static InMemoryStoryCatalog CreateCatalog(StoryTimeOptions? options = null)
    {
        options ??= StoryTimeOptionsFactory.Create();
        var optionsWrapper = Options.Create(options);
        var logger = NullLogger<ProceduralMediaAssetService>.Instance;
        var httpClientFactory = new StubHttpClientFactory();
        return new InMemoryStoryCatalog(optionsWrapper, new ProceduralMediaAssetService(optionsWrapper, logger, httpClientFactory));
    }

    private static StoryLibraryItem CreateItem(string storyId, bool favorite = false, DateTimeOffset? createdAt = null) => new()
    {
        StoryId = storyId,
        Title = $"Story {storyId}",
        Mode = "series",
        IsFavorite = favorite,
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow
    };

    [Fact]
    public void GetRecent_ReturnsEmptyForUnknownUser()
    {
        var catalog = CreateCatalog();

        var recent = catalog.GetRecent("unknown-user");

        Assert.Empty(recent);
    }

    [Fact]
    public void GetFavorites_ReturnsEmptyForUnknownUser()
    {
        var catalog = CreateCatalog();

        var favorites = catalog.GetFavorites("unknown-user");

        Assert.Empty(favorites);
    }

    [Fact]
    public void GetRecent_ReturnsAddedItemsInDescendingOrder()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-1", CreateItem("story-a", createdAt: DateTimeOffset.UtcNow.AddMinutes(-2)));
        catalog.Add("user-1", CreateItem("story-b", createdAt: DateTimeOffset.UtcNow.AddMinutes(-1)));
        catalog.Add("user-1", CreateItem("story-c", createdAt: DateTimeOffset.UtcNow));

        var recent = catalog.GetRecent("user-1");

        Assert.Equal(3, recent.Count);
        Assert.Equal("story-c", recent[0].StoryId);
    }

    [Fact]
    public void GetRecent_RespectsRecentItemsLimit()
    {
        var options = StoryTimeOptionsFactory.Create();
        options.Catalog.RecentItemsLimit = 2;
        var catalog = CreateCatalog(options);

        for (var i = 0; i < 5; i++)
        {
            catalog.Add("user-limit", CreateItem($"story-{i}", createdAt: DateTimeOffset.UtcNow.AddMinutes(i)));
        }

        var recent = catalog.GetRecent("user-limit");

        Assert.Equal(2, recent.Count);
    }

    [Fact]
    public void GetRecent_StripsAudioPayload()
    {
        var catalog = CreateCatalog();
        var item = CreateItem("story-audio");
        item.FullAudio = "data:audio/wav;base64,AAAA";
        catalog.Add("user-audio", item);

        var recent = catalog.GetRecent("user-audio");

        Assert.Single(recent);
        Assert.Null(recent[0].FullAudio);
    }

    [Fact]
    public void SetFavorite_MarksItemAsFavorite()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-fav", CreateItem("story-fav"));

        var result = catalog.SetFavorite("story-fav", true);

        Assert.True(result);
        var favorites = catalog.GetFavorites("user-fav");
        Assert.Single(favorites);
        Assert.Equal("story-fav", favorites[0].StoryId);
    }

    [Fact]
    public void SetFavorite_ReturnsFalseForUnknownStory()
    {
        var catalog = CreateCatalog();

        var result = catalog.SetFavorite("nonexistent", true);

        Assert.False(result);
    }

    [Fact]
    public void GetFavorites_ReturnsOnlyFavoritedItems()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-mixed", CreateItem("story-1"));
        catalog.Add("user-mixed", CreateItem("story-2"));
        catalog.Add("user-mixed", CreateItem("story-3"));
        catalog.SetFavorite("story-2", true);

        var favorites = catalog.GetFavorites("user-mixed");

        Assert.Single(favorites);
        Assert.Equal("story-2", favorites[0].StoryId);
    }

    [Fact]
    public void SetFavorite_CanUnfavoriteItem()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-unfav", CreateItem("story-unfav"));
        catalog.SetFavorite("story-unfav", true);
        catalog.SetFavorite("story-unfav", false);

        var favorites = catalog.GetFavorites("user-unfav");

        Assert.Empty(favorites);
    }

    [Fact]
    public void Snapshot_ReturnsAllItemsForUser()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-snap", CreateItem("story-a"));
        catalog.Add("user-snap", CreateItem("story-b"));
        catalog.Add("other-user", CreateItem("story-c"));

        var snapshot = catalog.Snapshot("user-snap");

        Assert.Equal(2, snapshot.Count);
    }

    [Fact]
    public void SetApproval_MarksStoryAsApproved()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-approve", CreateItem("story-approve"));

        var approved = catalog.SetApproval("user-approve", "story-approve");

        Assert.NotNull(approved);
        Assert.True(approved!.FullAudioReady);
        Assert.NotNull(approved.FullAudio);
        Assert.StartsWith("data:audio/wav;base64,", approved.FullAudio);
    }

    [Fact]
    public void SetApproval_ReturnsNullForUnknownStory()
    {
        var catalog = CreateCatalog();

        var approved = catalog.SetApproval("user-approve", "nonexistent");

        Assert.Null(approved);
    }

    [Fact]
    public void GetRecent_IsolatesUserData()
    {
        var catalog = CreateCatalog();
        catalog.Add("user-a", CreateItem("story-a"));
        catalog.Add("user-b", CreateItem("story-b"));

        var recentA = catalog.GetRecent("user-a");
        var recentB = catalog.GetRecent("user-b");

        Assert.Single(recentA);
        Assert.Equal("story-a", recentA[0].StoryId);
        Assert.Single(recentB);
        Assert.Equal("story-b", recentB[0].StoryId);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
