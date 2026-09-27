using Microsoft.Extensions.Options;

namespace OstrunCrawlerService.Api.Fetching;

public sealed class UrlFetcher(HttpClient httpClient, IOptions<CrawlerOptions> options)
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
    private readonly IReadOnlySet<string> _allowedHosts = options.Value.AllowedHostSet();

    public async Task<IReadOnlyList<FetchResult>> FetchAllAsync(IReadOnlyList<FetchTarget> targets, CancellationToken cancellationToken)
    {
        return await Task.WhenAll(targets.Select(target => FetchAsync(target, cancellationToken)));
    }

    private async Task<FetchResult> FetchAsync(FetchTarget target, CancellationToken cancellationToken)
    {
        if (!_allowedHosts.Contains(target.Url.IdnHost))
        {
            return Failed(target, FetchError.HostNotAllowed);
        }

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
            return Failed(target, FetchError.Timeout);
        }
        catch (HttpRequestException exception) when (exception.InnerException is BlockedAddressException)
        {
            return Failed(target, FetchError.HostNotAllowed);
        }
        catch (HttpRequestException)
        {
            return Failed(target, FetchError.NetworkError);
        }
    }

    private static FetchResult Failed(FetchTarget target, FetchError error) =>
        new(target.Id, target.Url.AbsoluteUri, null, false, error);
}
