using HackerNewsAPI_WebApplication.Model;

namespace HackerNewsAPI_WebApplication.Tests;

public sealed class ItemExtensionTests {
    [Fact]
    public void Maps_item_to_story() {
        var story = FakeHackerNews.Story(7, 1716).ToStory();

        Assert.Equal(
            new Story(
                Title: "Story 7",
                Uri: "https://example.com/7",
                PostedBy: "user7",
                Time: new DateTimeOffset(2019, 10, 12, 13, 43, 1, TimeSpan.Zero),
                Score: 1716,
                CommentCount: 70),
            story);
    }
}
