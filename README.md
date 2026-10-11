# Santander-DeveloperCodingTest

An ASP.NET Core (.NET 10) REST API that returns the best `n` stories from the
[Hacker News API](https://github.com/HackerNews/API), in descending order of score.

## Running the application

Prerequisite: the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project HackerNewsAPI-WebApplication
```

The API listens on `http://localhost:5166`:

```bash
curl "http://localhost:5166/api/story?n=10"
```

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

In the Development environment the OpenAPI document is served at `/openapi/v1.json`. You can also send requests with
`HackerNewsAPI-WebApplication/HackerNewsAPI-WebApplication.http` from Visual Studio, VS Code or Rider.

### Responses

| Status | When |
| --- | --- |
| `200 OK` | An array of up to `n` stories, highest score first. |
| `400 Bad Request` | `n` is missing, not an integer, or less than 1. |
| `503 Service Unavailable` | The app has just started and the first load from Hacker News hasn't finished within `ReadyTimeout`. Includes a `Retry-After` header. |

### Configuration

Settings live in the `HackerNewsOptions` section of `appsettings.json`. Environment variables can override them,
for example `HackerNewsOptions__RefreshInterval=00:05:00`.

| Setting | Default | Description |
| --- | --- | --- |
| `BaseAddress` | `https://hacker-news.firebaseio.com/v0/` | Hacker News API root. |
| `RefreshInterval` | `00:01:00` | How often the best stories are re-fetched from Hacker News. |
| `ReadyTimeout` | `00:01:00` | How long a request waits for the first load before returning `503`. |

## Design

The key requirement is to serve many requests without overloading Hacker News. To meet it, **request traffic
is decoupled from Hacker News traffic**:

- `HackerNewsBackgroundService` is a hosted background service. Every `RefreshInterval` it fetches `beststories.json`
  and the details of each story, then builds an array of stories sorted by score. It swaps the array in
  atomically.
- API requests only read the current array and return the first `n` stories. They make no calls to Hacker News.
  Load on Hacker News stays at about 201 requests per refresh interval, however many requests the API receives.
- Story details are fetched one at a time, so Hacker News never receives a burst of concurrent requests from this
  service.
- The `HttpClient` uses the standard resilience handler from `Microsoft.Extensions.Http.Resilience`. It provides
  retries with exponential backoff, timeouts and a circuit breaker.
- If a refresh fails, the previous snapshot keeps being served and the next interval tries again.

## Assumptions

- **Sorting:** the order of `beststories.json` is Hacker News' own ranking, not pure score order. The API
  therefore fetches every best story id (up to 200) and sorts them by score itself.
- **Large `n`:** Hacker News publishes at most about 200 best stories. If `n` is larger than the number available,
  the API returns all of them rather than an error.
- **Freshness:** results can be up to `RefreshInterval` (1 minute by default) old. Scores and comment counts
  change gradually, so this is a reasonable trade for protecting the upstream API.
- **Text posts:** posts such as "Ask HN" have no `url`. For these, `uri` links to the Hacker News discussion page
  (`https://news.ycombinator.com/item?id=…`).
- **Filtering:** items whose `type` isn't `story` (for example jobs) are excluded. Deleted or dead stories are not
  filtered out, because they rarely appear in `beststories.json`.
- **Field mapping:** `time` is converted from Unix seconds to an ISO 8601 UTC timestamp. `commentCount` is the item's
  `descendants` value, which is the total number of comments.
- **Deployment:** a single instance is assumed, so the cache is held in process memory.

## Enhancements given more time

- **Tests:** unit tests for the background service using a fake `IHackerNews`, and integration tests with
  `WebApplicationFactory`.
- **Faster refreshes:** fetch story details with bounded parallelism (for example 4–8 concurrent requests). The first
  load would take 1–2 seconds instead of about 30.
- **Incremental refresh:** only re-fetch new ids, or use the Hacker News `updates.json` endpoint to re-fetch only
  items that have changed.
- **Shared cache for scale-out:** with several instances, each one polls Hacker News independently. A single
  refresher writing to Redis (or using `HybridCache` with a distributed store) would fix that.
- **HTTP caching:** `Cache-Control` / `ETag` headers or ASP.NET Core output caching, so clients and CDNs can avoid
  repeat requests.
- **Rate limiting:** limit requests per client to protect this API.
- **Observability:** a health check that reports unhealthy when the snapshot is too old, plus OpenTelemetry metrics
  and traces.
- **Packaging:** a Dockerfile and a CI pipeline.
