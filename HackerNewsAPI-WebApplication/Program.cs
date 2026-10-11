using HackerNewsAPI_WebApplication.HackerNews;
using HackerNewsAPI_WebApplication.Service;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var serviceCollection = builder.Services;
serviceCollection.AddControllers();
// One HackerNewsBackgroundService instance is shared as the hosted service that refreshes the cache
// and as the IHackerNewsBackgroundService that the controller reads from.
serviceCollection
    .AddHostedService(sp => sp.GetRequiredService<HackerNewsBackgroundService>())
    .AddOpenApi()
    .AddSingleton<IHackerNewsBackgroundService>(sp => sp.GetRequiredService<HackerNewsBackgroundService>())
    .AddSingleton<HackerNewsBackgroundService>()
    .AddSingleton(TimeProvider.System)
    .AddOptions<HackerNewsOptions>()
    .Bind(builder.Configuration.GetSection(nameof(HackerNewsOptions)))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// The standard resilience handler adds retries with backoff, timeouts and a circuit breaker.
serviceCollection.AddHttpClient<IHackerNews, HackerNews>((sp, c) =>c.BaseAddress = sp.GetRequiredService<IOptions<HackerNewsOptions>>().Value.BaseAddress).AddStandardResilienceHandler();

var app = builder.Build();
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
