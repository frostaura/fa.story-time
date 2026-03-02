using StoryTime.Api.Domain;

namespace StoryTime.Api.Services;

public interface IStoryCatalog
{
    void Add(string softUserId, StoryLibraryItem item);

    StoryLibraryItem? SetApproval(string storyId);

    bool SetFavorite(string storyId, bool isFavorite);

    IReadOnlyList<StoryLibraryItem> GetRecent(string softUserId);

    IReadOnlyList<StoryLibraryItem> GetFavorites(string softUserId);

    IReadOnlyList<StoryLibraryItem> Snapshot(string softUserId);
}
