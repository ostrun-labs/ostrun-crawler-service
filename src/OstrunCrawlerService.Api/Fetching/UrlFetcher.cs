using Microsoft.Extensions.Options;

namespace OstrunCrawlerService.Api.Fetching;

public sealed class UrlFetcher(HttpClient httpClient, IOptions<CrawlerOptions> options)
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);

    public async Task<IReadOnlyList<FetchResult>> FetchAllAsync(IReadOnlyList<FetchTarget> targets, CancellationToken cancellationToken)
    {
        return await Task.WhenAll(targets.Select(target => FetchAsync(target, cancellationToken)));
    }

    private async Task<FetchResult> FetchAsync(FetchTarget target, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, target.Url);
            // Only the status line matters, so the body is never downloaded.
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            var status = (int)response.StatusCode;
            return new FetchResult(target.Id, target.Url.AbsoluteUri, status, status == target.ExpectedStatus, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new FetchResult(target.Id, target.Url.AbsoluteUri, null, false, FetchError.Timeout);
        }
        catch (HttpRequestException)
        {
            return new FetchResult(target.Id, target.Url.AbsoluteUri, null, false, FetchError.NetworkError);
        }
    }
}
