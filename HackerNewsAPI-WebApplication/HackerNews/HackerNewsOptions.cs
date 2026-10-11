using System.ComponentModel.DataAnnotations;

namespace HackerNewsAPI_WebApplication.HackerNews;

public sealed class HackerNewsOptions {
    [Required]
    public Uri BaseAddress { get; set; } = new(uriString: "https://hacker-news.firebaseio.com/v0/");

    /// <summary>
    /// How long a request waits for the first cache load before the API returns 503.
    /// </summary>
    [Range(typeof(TimeSpan), minimum: "00:00:01", maximum: "00:01:00")]
    public TimeSpan ReadyTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How often the cache is refreshed from Hacker News.
    /// </summary>
    [Range(typeof(TimeSpan), minimum: "00:01:00", maximum: "1.00:00:00")]
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
}