using HackerNewsAPI_WebApplication.HackerNews;
using HackerNewsAPI_WebApplication.Model;

namespace HackerNewsAPI_WebApplication.Tests;

/// <summary>In-memory stand-in for the Hacker News API that counts the calls made to it.</summary>
internal sealed class FakeHackerNews :
    IHackerNews {
    private int itemCallCount;
    private int storyIdentifierCallCount;

    public List<int> StoryIdentifierCollection { get; } = [];

    public Dictionary<int, Item?> ItemCollection { get; } = [];

    public Exception? Failure { get; set; }

    public int ItemCallCount => itemCallCount;

    public int StoryIdentifierCallCount => storyIdentifierCallCount;

    public void AddStory(Item item) {
        StoryIdentifierCollection.Add(item.Id);
        ItemCollection[item.Id] = item;
    }

    public Task<Item?> GetItemAsync(int id, CancellationToken cancellationToken) {
        Interlocked.Increment(ref itemCallCount);
        return Task.FromResult(ItemCollection.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<int>> GetStoryIdentifierCollectionAsync(CancellationToken cancellationToken) {
        Interlocked.Increment(ref storyIdentifierCallCount);
        if (Failure is not null) {
            throw Failure;
        }

        return Task.FromResult<IReadOnlyList<int>>(StoryIdentifierCollection.ToArray());
    }

    public static Item Story(int id, int score) =>
        new() {
            By = $"user{id}",
            Descendants = id * 10,
            Id = id,
            Score = score,
            Time = 1570887781,
            Title = $"Story {id}",
            Type = "story",
            Url = $"https://example.com/{id}"
        };
}
