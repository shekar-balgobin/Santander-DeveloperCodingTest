using HackerNewsAPI_WebApplication.Model;
using HackerNewsAPI_WebApplication.Service;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HackerNewsAPI_WebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StoryController(IHackerNewsBackgroundService hackerNewsBackgroundService) :
    ControllerBase {
    /// <summary>
    /// The best <paramref name="n"/> Hacker News stories in descending order of score.
    /// </summary>
    /// <param name="n">Number of stories to return.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Story>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<Story>>> Get([FromQuery, Required, Range(1, int.MaxValue)] int? n, CancellationToken cancellationToken) {
        var storyCollection = await hackerNewsBackgroundService.GetBestStoryCollectionAsync(n!.Value, cancellationToken);
        if (storyCollection is null) {
            Response.Headers.RetryAfter = "5";
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Best stories have not been loaded from Hacker News yet. Please retry shortly.");
        }

        return Ok(storyCollection);
    }
}