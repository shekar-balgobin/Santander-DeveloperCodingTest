namespace HackerNewsAPI_WebApplication.Model;

public sealed record Story(
    string? Title,
    string? Uri,
    string? PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount);
