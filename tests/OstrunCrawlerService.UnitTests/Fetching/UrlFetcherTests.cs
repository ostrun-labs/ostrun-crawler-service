using System.Net;
using Microsoft.Extensions.Options;
using OstrunCrawlerService.Api.Fetching;

namespace OstrunCrawlerService.UnitTests.Fetching;

public class UrlFetcherTests
{
    private static UrlFetcher CreateFetcher(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond, double timeoutSeconds = 5) =>
        new(
            new HttpClient(new FakeHandler(respond)),
            Options.Create(new CrawlerOptions
            {
                TimeoutSeconds = timeoutSeconds,
                AllowedHosts = "registry.npmjs.org,free.test,down.test,up.test,slow.test,a.test",
            }));

    private static FetchTarget Target(string id, string url, int expectedStatus = 404) => new(id, new Uri(url), expectedStatus);

    [Fact]
    public async Task Reports_matched_when_status_equals_expected()
    {
        var fetcher = CreateFetcher((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var results = await fetcher.FetchAllAsync([Target("npm", "https://registry.npmjs.org/free-name")], CancellationToken.None);

        Assert.Equal([new FetchResult("npm", "https://registry.npmjs.org/free-name", 404, true, null)], results);
    }

    [Fact]
    public async Task Reports_not_matched_when_status_differs()
    {
        var fetcher = CreateFetcher((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var results = await fetcher.FetchAllAsync([Target("npm", "https://registry.npmjs.org/react")], CancellationToken.None);

        Assert.Equal([new FetchResult("npm", "https://registry.npmjs.org/react", 200, false, null)], results);
    }

    [Fact]
    public async Task Keeps_request_order_and_isolates_failures()
    {
        var fetcher = CreateFetcher((request, _) => request.RequestUri!.Host switch
        {
            "down.test" => throw new HttpRequestException("connection refused"),
            "free.test" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
        });

        var results = await fetcher.FetchAllAsync(
            [Target("a", "https://free.test/"), Target("b", "https://down.test/"), Target("c", "https://up.test/")],
            CancellationToken.None);

        Assert.Equal(
            [
                new FetchResult("a", "https://free.test/", 404, true, null),
                new FetchResult("b", "https://down.test/", null, false, FetchError.NetworkError),
                new FetchResult("c", "https://up.test/", 200, false, null),
            ],
            results);
    }

    [Fact]
    public async Task Reports_timeout_when_target_is_too_slow()
    {
        var fetcher = CreateFetcher(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            timeoutSeconds: 0.05);

        var results = await fetcher.FetchAllAsync([Target("slow", "https://slow.test/")], CancellationToken.None);

        Assert.Equal([new FetchResult("slow", "https://slow.test/", null, false, FetchError.Timeout)], results);
    }

    [Fact]
    public async Task Propagates_cancellation_from_the_caller()
    {
        var fetcher = CreateFetcher(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var callerGone = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fetcher.FetchAllAsync([Target("a", "https://a.test/")], callerGone.Token));
    }

    [Theory]
    [InlineData("https://not-listed.test/")]
    [InlineData("https://sub.a.test/")]
    public async Task Refuses_hosts_outside_the_allowlist_without_sending_a_request(string url)
    {
        var sent = false;
        var fetcher = CreateFetcher((_, _) =>
        {
            sent = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var results = await fetcher.FetchAllAsync([Target("x", url)], CancellationToken.None);

        Assert.Equal([new FetchResult("x", url, null, false, FetchError.HostNotAllowed)], results);
        Assert.False(sent);
    }

    [Fact]
    public async Task Matches_allowlisted_hosts_case_insensitively()
    {
        var fetcher = CreateFetcher((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var results = await fetcher.FetchAllAsync([Target("npm", "https://REGISTRY.npmjs.org/x")], CancellationToken.None);

        Assert.Equal(404, Assert.Single(results).Status);
    }

    [Fact]
    public async Task Empty_allowlist_refuses_every_host()
    {
        var fetcher = new UrlFetcher(
            new HttpClient(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))),
            Options.Create(new CrawlerOptions()));

        var results = await fetcher.FetchAllAsync([Target("npm", "https://registry.npmjs.org/x")], CancellationToken.None);

        Assert.Equal(FetchError.HostNotAllowed, Assert.Single(results).Error);
    }

    [Fact]
    public async Task Reports_host_not_allowed_when_the_address_is_blocked_at_connect_time()
    {
        var fetcher = CreateFetcher((request, _) =>
            throw new HttpRequestException("blocked", new BlockedAddressException(request.RequestUri!.Host)));

        var results = await fetcher.FetchAllAsync([Target("a", "https://a.test/")], CancellationToken.None);

        Assert.Equal([new FetchResult("a", "https://a.test/", null, false, FetchError.HostNotAllowed)], results);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
