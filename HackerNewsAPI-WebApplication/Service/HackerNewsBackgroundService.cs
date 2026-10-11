using HackerNewsAPI_WebApplication.HackerNews;
using HackerNewsAPI_WebApplication.Model;
using Microsoft.Extensions.Options;

namespace HackerNewsAPI_WebApplication.Service;

/// <summary>
/// Periodically fetches the best stories from Hacker News and caches them in memory, sorted by score.
/// Requests are served from the cache only, so the load on Hacker News does not grow with request volume.
/// </summary>
public sealed class HackerNewsBackgroundService(IHackerNews hackerNews, ILogger<HackerNewsBackgroundService> logger, IOptions<HackerNewsOptions> options, TimeProvider timeProvider) :
    BackgroundService,
    IHackerNewsBackgroundService {
    // Completed after the first successful refresh; requests wait on it until the cache has data.
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Replaced as a whole on each refresh and never modified, so readers need no lock.
    private volatile Story[] storyCollection = [];

    #region BackgroundService implementation.

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        using var periodicTimer = new PeriodicTimer(options.Value.RefreshInterval, timeProvider);

        do {
            try {
                await RefreshCacheAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested) {
                // Keep serving the previous cache; the next tick tries again.
                logger.LogError(exception, message: "Failed to refresh cache");
            }
        } while (await periodicTimer.WaitForNextTickAsync(stoppingToken));
    }

    #endregion

    #region IHackerNewsBackgroundService implementation.

    public async Task<IReadOnlyList<Story>?> GetBestStoryCollectionAsync(int count, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        try {
            await ready.Task.WaitAsync(options.Value.ReadyTimeout, timeProvider, cancellationToken);
        }
        catch (TimeoutException) {
            return null;
        }

        // Read the field once so a concurrent refresh can't mix two snapshots.
        var stories = storyCollection;
        return stories.Length <= count ? stories : stories[..count];
    }


    #endregion

    public async Task RefreshCacheAsync(CancellationToken cancellationToken) {
        var identifierCollection = await hackerNews.GetStoryIdentifierCollectionAsync(cancellationToken);
        var itemCollection = new List<Item>(identifierCollection.Count);

        // Fetched one at a time so Hacker News never receives a burst of concurrent requests.
        foreach (var identifier in identifierCollection) {
            var item = await hackerNews.GetItemAsync(identifier, cancellationToken);
            if (item is not null) {
                itemCollection.Add(item);
            }
        }

        // beststories.json is in Hacker News' ranking order, not score order, so sort here.
        var storyCollection = itemCollection
            .Where(item => item.Type == "story")
            .Select(item => item.ToStory())
            .OrderByDescending(story => story.Score)
            .ToArray();

        this.storyCollection = storyCollection;
        ready.TrySetResult();
        logger.LogInformation("Refreshed cache with {Count} stories", storyCollection.Length);
    }
}
