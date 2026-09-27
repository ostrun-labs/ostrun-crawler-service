namespace OstrunCrawlerService.Api.Fetching;

public sealed record FetchRequest(IReadOnlyList<FetchTargetRequest>? Targets);

public sealed record FetchTargetRequest(string? Id, string? Url, MatchRequest? Match);

public sealed record MatchRequest(int? Status);

public sealed record FetchResponse(IReadOnlyList<FetchResult> Results);

public sealed record FetchResult(string Id, string Url, int? Status, bool Matched, FetchError? Error);

public enum FetchError
{
    Timeout,
    NetworkError,
}

public sealed record FetchTarget(string Id, Uri Url, int ExpectedStatus);
