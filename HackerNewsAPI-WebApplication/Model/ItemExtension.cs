namespace HackerNewsAPI_WebApplication.Model;

public static class ItemExtension {
    public static Model.Story ToStory(this Item item) =>
        new(
            Title: item.Title,
            Uri: item.Url,
            PostedBy: item.By,
            Time: DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score: item.Score,
            CommentCount: item.Descendants);
}