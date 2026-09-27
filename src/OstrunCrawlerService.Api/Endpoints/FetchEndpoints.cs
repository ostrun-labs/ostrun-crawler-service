using Microsoft.Extensions.Options;
using OstrunCrawlerService.Api.Fetching;

namespace OstrunCrawlerService.Api.Endpoints;

public static class FetchEndpoints
{
    public static IEndpointRouteBuilder MapFetchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/crawler/fetch", async (FetchRequest request, UrlFetcher fetcher, IOptions<CrawlerOptions> options, CancellationToken cancellationToken) =>
        {
            if (!FetchRequestParser.TryParse(request, options.Value.MaxTargets, out var targets, out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var results = await fetcher.FetchAllAsync(targets, cancellationToken);
            return Results.Ok(new FetchResponse(results));
        });

        return app;
    }
}
