namespace HackerNewsAPI_WebApplication.HackerNews;

public sealed class HackerNews(HttpClient httpClient) :
    IHackerNews {
    public async Task<IReadOnlyList<int>> GetStoryIdentifierCollectionAsync(CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<int[]>(requestUri: "beststories.json", cancellationToken) ?? [];

    public async Task<Model.Item?> GetItemAsync(int id, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<Model.Item>(requestUri: $"item/{id}.json", cancellationToken);
}