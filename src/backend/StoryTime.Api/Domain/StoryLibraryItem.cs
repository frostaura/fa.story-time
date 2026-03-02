namespace StoryTime.Api.Domain;

public sealed class StoryLibraryItem
{
    public required string StoryId { get; init; }

    public required string Title { get; init; }

    public required string Mode { get; init; }

    public string? SeriesId { get; init; }

    public bool IsFavorite { get; set; }

    public bool FullAudioReady { get; set; }

    public string? FullAudio { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
}
