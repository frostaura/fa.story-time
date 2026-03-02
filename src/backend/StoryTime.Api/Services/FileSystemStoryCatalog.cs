using System.Text.Json;
using Microsoft.Extensions.Options;
using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public sealed class FileSystemStoryCatalog(IOptions<StoryTimeOptions> options, IMediaAssetService mediaAssetService) : IStoryCatalog
{
    private readonly StoryTimeOptions _options = options.Value;
    private readonly IMediaAssetService _mediaAssetService = mediaAssetService;
    private readonly object _syncRoot = new();
    private readonly string _filePath = ResolveFilePath(options.Value.Catalog.FilePath);
    private readonly Dictionary<string, List<StoryLibraryItem>> _storiesByUser = LoadCatalog(options.Value.Catalog.FilePath);

    public void Add(string softUserId, StoryLibraryItem item)
    {
        lock (_syncRoot)
        {
            if (!_storiesByUser.TryGetValue(softUserId, out var stories))
            {
                stories = [];
                _storiesByUser[softUserId] = stories;
            }

            stories.Insert(0, Clone(item, stripAudioPayload: true));
            PersistUnsafe();
        }
    }

    public StoryLibraryItem? SetApproval(string storyId)
    {
        lock (_syncRoot)
        {
            foreach (var stories in _storiesByUser.Values)
            {
                var story = stories.FirstOrDefault(s => s.StoryId == storyId);
                if (story is null)
                {
                    continue;
                }

                story.FullAudioReady = true;
                PersistUnsafe();

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
        lock (_syncRoot)
        {
            foreach (var stories in _storiesByUser.Values)
            {
                var story = stories.FirstOrDefault(s => s.StoryId == storyId);
                if (story is null)
                {
                    continue;
                }

                story.IsFavorite = isFavorite;
                PersistUnsafe();
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<StoryLibraryItem> GetRecent(string softUserId)
    {
        lock (_syncRoot)
        {
            if (!_storiesByUser.TryGetValue(softUserId, out var stories))
            {
                return [];
            }

            return stories
                .OrderByDescending(s => s.CreatedAt)
                .Take(Math.Max(1, _options.Catalog.RecentItemsLimit))
                .Select(item => Clone(item, stripAudioPayload: true))
                .ToArray();
        }
    }

    public IReadOnlyList<StoryLibraryItem> GetFavorites(string softUserId)
    {
        lock (_syncRoot)
        {
            if (!_storiesByUser.TryGetValue(softUserId, out var stories))
            {
                return [];
            }

            return stories
                .Where(s => s.IsFavorite)
                .OrderByDescending(s => s.CreatedAt)
                .Select(item => Clone(item, stripAudioPayload: true))
                .ToArray();
        }
    }

    public IReadOnlyList<StoryLibraryItem> Snapshot(string softUserId)
    {
        lock (_syncRoot)
        {
            if (!_storiesByUser.TryGetValue(softUserId, out var stories))
            {
                return [];
            }

            return stories.Select(item => Clone(item, stripAudioPayload: true)).ToArray();
        }
    }

    private static Dictionary<string, List<StoryLibraryItem>> LoadCatalog(string configuredPath)
    {
        var path = ResolveFilePath(configuredPath);
        if (!File.Exists(path))
        {
            return new Dictionary<string, List<StoryLibraryItem>>(StringComparer.Ordinal);
        }

        var raw = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new Dictionary<string, List<StoryLibraryItem>>(StringComparer.Ordinal);
        }

        var loaded = JsonSerializer.Deserialize<Dictionary<string, List<StoryLibraryItem>>>(raw);
        return loaded is null
            ? new Dictionary<string, List<StoryLibraryItem>>(StringComparer.Ordinal)
            : new Dictionary<string, List<StoryLibraryItem>>(loaded, StringComparer.Ordinal);
    }

    private void PersistUnsafe()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var persisted = _storiesByUser.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(item => Clone(item, stripAudioPayload: true)).ToList(),
            StringComparer.Ordinal);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(persisted));
    }

    private static string ResolveFilePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, configuredPath);
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
