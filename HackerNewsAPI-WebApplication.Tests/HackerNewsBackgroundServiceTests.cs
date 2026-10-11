using HackerNewsAPI_WebApplication.HackerNews;
using HackerNewsAPI_WebApplication.Service;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HackerNewsAPI_WebApplication.Tests;

public sealed class HackerNewsBackgroundServiceTests {
    private readonly FakeHackerNews hackerNews = new();

    private HackerNewsBackgroundService CreateService(TimeSpan? readyTimeout = null) =>
        new(
            hackerNews,
            NullLogger<HackerNewsBackgroundService>.Instance,
            Options.Create(new HackerNewsOptions { ReadyTimeout = readyTimeout ?? TimeSpan.FromSeconds(5) }),
            TimeProvider.System);

    [Fact]
    public async Task Returns_stories_in_descending_order_of_score() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 50));
        hackerNews.AddStory(FakeHackerNews.Story(2, 300));
        hackerNews.AddStory(FakeHackerNews.Story(3, 120));
        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        var storyCollection = await service.GetBestStoryCollectionAsync(10, CancellationToken.None);

        Assert.Equal([300, 120, 50], storyCollection!.Select(story => story.Score));
    }

    [Fact]
    public async Task Returns_only_the_requested_number_of_stories() {
        for (var id = 1; id <= 20; id++) {
            hackerNews.AddStory(FakeHackerNews.Story(id, id));
        }

        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        var storyCollection = await service.GetBestStoryCollectionAsync(3, CancellationToken.None);

        Assert.Equal([20, 19, 18], storyCollection!.Select(story => story.Score));
    }

    [Fact]
    public async Task Returns_all_stories_when_count_exceeds_number_available() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 10));
        hackerNews.AddStory(FakeHackerNews.Story(2, 20));
        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        var storyCollection = await service.GetBestStoryCollectionAsync(500, CancellationToken.None);

        Assert.Equal(2, storyCollection!.Count);
    }

    [Fact]
    public async Task Excludes_items_that_are_not_stories() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 10));
        hackerNews.AddStory(FakeHackerNews.Story(2, 20) with { Type = "job" });
        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        var story = Assert.Single((await service.GetBestStoryCollectionAsync(10, CancellationToken.None))!);

        Assert.Equal(10, story.Score);
    }

    [Fact]
    public async Task Skips_identifiers_that_return_no_item() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 10));
        hackerNews.StoryIdentifierCollection.Add(99);
        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        var story = Assert.Single((await service.GetBestStoryCollectionAsync(10, CancellationToken.None))!);

        Assert.Equal(10, story.Score);
    }

    [Fact]
    public async Task Waiting_request_completes_once_first_refresh_finishes() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 10));
        var service = CreateService();

        var pending = service.GetBestStoryCollectionAsync(1, CancellationToken.None);
        Assert.False(pending.IsCompleted);
        await service.RefreshCacheAsync(CancellationToken.None);

        Assert.Single((await pending)!);
    }

    [Fact]
    public async Task Returns_null_when_cache_is_not_loaded_within_ready_timeout() {
        var service = CreateService(readyTimeout: TimeSpan.FromMilliseconds(50));

        Assert.Null(await service.GetBestStoryCollectionAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Keeps_previous_cache_when_refresh_fails() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 10));
        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        hackerNews.Failure = new HttpRequestException("Hacker News is unavailable");
        await Assert.ThrowsAsync<HttpRequestException>(() => service.RefreshCacheAsync(CancellationToken.None));

        Assert.Single((await service.GetBestStoryCollectionAsync(1, CancellationToken.None))!);
    }

    [Fact]
    public async Task Serving_requests_does_not_call_hacker_news() {
        for (var id = 1; id <= 200; id++) {
            hackerNews.AddStory(FakeHackerNews.Story(id, id));
        }

        var service = CreateService();
        await service.RefreshCacheAsync(CancellationToken.None);

        await Task.WhenAll(Enumerable.Range(0, 1000).Select(_ => service.GetBestStoryCollectionAsync(200, CancellationToken.None)));

        Assert.Equal(1, hackerNews.StoryIdentifierCallCount);
        Assert.Equal(200, hackerNews.ItemCallCount);
    }

    [Fact]
    public async Task Starting_the_service_loads_the_cache() {
        hackerNews.AddStory(FakeHackerNews.Story(1, 10));
        var service = CreateService();

        await service.StartAsync(CancellationToken.None);
        var storyCollection = await service.GetBestStoryCollectionAsync(1, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Single(storyCollection!);
    }
}
