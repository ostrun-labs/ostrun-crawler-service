using OstrunCrawlerService.Api.Fetching;

namespace OstrunCrawlerService.UnitTests.Fetching;

public class FetchRequestParserTests
{
    private static FetchTargetRequest Target(string? id = "npm", string? url = "https://registry.npmjs.org/x", int? status = 404) =>
        new(id, url, status is null ? null : new MatchRequest(status));

    [Fact]
    public void Parses_valid_targets_in_order()
    {
        var request = new FetchRequest([Target("a", "https://a.test/x"), Target("b", "http://b.test/y", 200)]);

        var ok = FetchRequestParser.TryParse(request, 20, out var targets, out var errors);

        Assert.True(ok);
        Assert.Empty(errors);
        Assert.Equal(
            [new FetchTarget("a", new Uri("https://a.test/x"), 404), new FetchTarget("b", new Uri("http://b.test/y"), 200)],
            targets);
    }

    [Fact]
    public void Rejects_missing_or_empty_targets()
    {
        Assert.False(FetchRequestParser.TryParse(new FetchRequest(null), 20, out _, out var nullErrors));
        Assert.Contains("targets", nullErrors.Keys);

        Assert.False(FetchRequestParser.TryParse(new FetchRequest([]), 20, out _, out var emptyErrors));
        Assert.Contains("targets", emptyErrors.Keys);
    }

    [Fact]
    public void Rejects_more_targets_than_allowed()
    {
        var request = new FetchRequest([Target("a"), Target("b"), Target("c")]);

        Assert.False(FetchRequestParser.TryParse(request, 2, out _, out var errors));
        Assert.Contains("targets", errors.Keys);
    }

    [Fact]
    public void Rejects_duplicate_ids()
    {
        var request = new FetchRequest([Target("npm"), Target("npm")]);

        Assert.False(FetchRequestParser.TryParse(request, 20, out _, out var errors));
        Assert.Equal(["targets[1]"], errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("registry.npmjs.org/x")]
    [InlineData("ftp://example.test/file")]
    [InlineData("file:///etc/passwd")]
    public void Rejects_urls_that_are_not_absolute_http(string? url)
    {
        var request = new FetchRequest([Target(url: url)]);

        Assert.False(FetchRequestParser.TryParse(request, 20, out _, out var errors));
        Assert.Contains("targets[0]", errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(99)]
    [InlineData(600)]
    public void Rejects_missing_or_out_of_range_status(int? status)
    {
        var request = new FetchRequest([Target(status: status)]);

        Assert.False(FetchRequestParser.TryParse(request, 20, out _, out var errors));
        Assert.Contains("targets[0]", errors.Keys);
    }

    [Fact]
    public void Rejects_missing_id()
    {
        var request = new FetchRequest([Target(id: " ")]);

        Assert.False(FetchRequestParser.TryParse(request, 20, out _, out var errors));
        Assert.Contains("targets[0]", errors.Keys);
    }
}
