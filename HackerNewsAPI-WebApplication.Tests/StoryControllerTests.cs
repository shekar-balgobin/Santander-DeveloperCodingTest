using HackerNewsAPI_WebApplication.Controllers;
using HackerNewsAPI_WebApplication.Model;
using HackerNewsAPI_WebApplication.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsAPI_WebApplication.Tests;

public sealed class StoryControllerTests {
    private static StoryController CreateController(IReadOnlyList<Story>? storyCollection) =>
        new(new StubHackerNewsBackgroundService(storyCollection)) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = new StubProblemDetailsFactory()
        };

    [Fact]
    public async Task Returns_ok_with_stories() {
        Story[] storyCollection = [FakeHackerNews.Story(1, 10).ToStory()];
        var controller = CreateController(storyCollection);

        var result = await controller.Get(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(storyCollection, ok.Value);
    }

    [Fact]
    public async Task Returns_service_unavailable_with_retry_after_when_cache_is_not_loaded() {
        var controller = CreateController(null);

        var result = await controller.Get(1, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
        Assert.Equal("5", controller.Response.Headers.RetryAfter);
    }

    private sealed class StubHackerNewsBackgroundService(IReadOnlyList<Story>? storyCollection) :
        IHackerNewsBackgroundService {
        public Task<IReadOnlyList<Story>?> GetBestStoryCollectionAsync(int count, CancellationToken cancellationToken) =>
            Task.FromResult(storyCollection);
    }

    private sealed class StubProblemDetailsFactory :
        Microsoft.AspNetCore.Mvc.Infrastructure.ProblemDetailsFactory {
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null) =>
            new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
    }
}
