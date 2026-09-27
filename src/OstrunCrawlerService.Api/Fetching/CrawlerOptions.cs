using System.ComponentModel.DataAnnotations;

namespace OstrunCrawlerService.Api.Fetching;

public sealed class CrawlerOptions
{
    public const string SectionName = "Crawler";

    [Range(0.01, 60)]
    public double TimeoutSeconds { get; init; } = 5;

    [Range(1, 100)]
    public int MaxTargets { get; init; } = 20;

    [Required]
    public string UserAgent { get; init; } = "OstrunCrawler/0.1 (+https://github.com/ostrun-labs/ostrun-crawler-service)";

    // Comma-separated host names, matched exactly. Empty means no host can be fetched.
    public string AllowedHosts { get; init; } = "";

    public IReadOnlySet<string> AllowedHostSet() =>
        AllowedHosts
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
