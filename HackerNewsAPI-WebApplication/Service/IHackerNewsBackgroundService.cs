using HackerNewsAPI_WebApplication.Model;

namespace HackerNewsAPI_WebApplication.Service;

public interface IHackerNewsBackgroundService {
    /// <summary>
    /// Returns up to <paramref name="count"/> best stories in descending order of score,
    /// or <c>null</c> if the cache has not been loaded within the ready timeout.
    /// </summary>
    Task<IReadOnlyList<Story>?> GetBestStoryCollectionAsync(int count, CancellationToken cancellationToken);
}