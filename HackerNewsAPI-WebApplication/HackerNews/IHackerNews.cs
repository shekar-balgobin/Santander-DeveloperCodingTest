namespace HackerNewsAPI_WebApplication.HackerNews;

public interface IHackerNews {
    Task<Model.Item?> GetItemAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> GetStoryIdentifierCollectionAsync(CancellationToken cancellationToken);
}
