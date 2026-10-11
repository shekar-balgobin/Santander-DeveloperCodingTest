namespace HackerNewsAPI_WebApplication.Model;

/// <summary>
/// An item as returned by the Hacker News API (https://hacker-news.firebaseio.com/v0/item/{id}.json).
/// </summary>
public sealed record Item {
    public string? By { get; init; }

    public int Descendants { get; init; }

    public int Id { get; init; }

    public int Score { get; init; }

    public long Time { get; init; }

    public string? Title { get; init; }

    public string? Type { get; init; }

    public string? Url { get; init; }
}
