using StoryTime.Api.Domain;

namespace StoryTime.Api.Contracts;

public sealed record LibraryResponse(
    IReadOnlyList<StoryLibraryItem> Recent,
    IReadOnlyList<StoryLibraryItem> Favorites,
    bool KidModeEnabled);
