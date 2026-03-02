using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public sealed class InMemoryStoryCatalog(IOptions<StoryTimeOptions> options, IMediaAssetService mediaAssetService) : IStoryCatalog
{
    private readonly StoryTimeOptions _options = options.Value;
    private readonly IMediaAssetService _mediaAssetService = mediaAssetService;
    private readonly ConcurrentDictionary<string, List<StoryLibraryItem>> _storiesByUser = new(StringComparer.Ordinal);

    public void Add(string softUserId, StoryLibraryItem item)
    {
        var stories = _storiesByUser.GetOrAdd(softUserId, _ => new List<StoryLibraryItem>());
        lock (stories)
        {
            stories.Insert(0, Clone(item, stripAudioPayload: true));
        }
    }

    public StoryLibraryItem? SetApproval(string storyId)
    {
        foreach (var stories in _storiesByUser.Values)
        {
            lock (stories)
            {
                var story = stories.FirstOrDefault(s => s.StoryId == storyId);
                if (story is null)
                {
                    continue;
                }

                story.FullAudioReady = true;
                var approved = Clone(story, stripAudioPayload: true);
                approved.FullAudio = _mediaAssetService.BuildAudioDataUri(
                    story.StoryId,
                    _options.Generation.FullDurationSeconds,
                    _options.Generation.FullAudioAmplitudeScale);
                return approved;
            }
        }

        return null;
    }

    public bool SetFavorite(string storyId, bool isFavorite)
    {
        foreach (var stories in _storiesByUser.Values)
        {
            lock (stories)
            {
                var story = stories.FirstOrDefault(s => s.StoryId == storyId);
                if (story is null)
                {
                    continue;
                }

                story.IsFavorite = isFavorite;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<StoryLibraryItem> GetRecent(string softUserId)
    {
        if (!_storiesByUser.TryGetValue(softUserId, out var stories))
        {
            return [];
        }

        lock (stories)
        {
            return stories
                .OrderByDescending(s => s.CreatedAt)
                .Take(Math.Max(1, _options.Catalog.RecentItemsLimit))
                .Select(item => Clone(item, stripAudioPayload: true))
                .ToArray();
        }
    }

    public IReadOnlyList<StoryLibraryItem> GetFavorites(string softUserId)
    {
        if (!_storiesByUser.TryGetValue(softUserId, out var stories))
        {
            return [];
        }

        lock (stories)
        {
            return stories.Where(s => s.IsFavorite).OrderByDescending(s => s.CreatedAt).Select(item => Clone(item, stripAudioPayload: true)).ToArray();
        }
    }

    public IReadOnlyList<StoryLibraryItem> Snapshot(string softUserId)
    {
        if (!_storiesByUser.TryGetValue(softUserId, out var stories))
        {
            return [];
        }

        lock (stories)
        {
            return stories.Select(item => Clone(item, stripAudioPayload: true)).ToArray();
        }
    }

    private static StoryLibraryItem Clone(StoryLibraryItem source, bool stripAudioPayload) => new()
    {
        StoryId = source.StoryId,
        Title = source.Title,
        Mode = source.Mode,
        SeriesId = source.SeriesId,
        IsFavorite = source.IsFavorite,
        FullAudioReady = source.FullAudioReady,
        FullAudio = stripAudioPayload ? null : source.FullAudio,
        CreatedAt = source.CreatedAt
    };
}
