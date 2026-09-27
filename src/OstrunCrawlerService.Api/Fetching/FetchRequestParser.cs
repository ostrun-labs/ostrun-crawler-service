namespace OstrunCrawlerService.Api.Fetching;

public static class FetchRequestParser
{
    public static bool TryParse(
        FetchRequest request,
        int maxTargets,
        out IReadOnlyList<FetchTarget> targets,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();
        var parsed = new List<FetchTarget>();
        targets = parsed;

        if (request.Targets is null || request.Targets.Count == 0)
        {
            errors["targets"] = ["At least one target is required."];
            return false;
        }

        if (request.Targets.Count > maxTargets)
        {
            errors["targets"] = [$"At most {maxTargets} targets are allowed per request."];
            return false;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < request.Targets.Count; i++)
        {
            var target = request.Targets[i];
            var key = $"targets[{i}]";
            var targetErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(target.Id))
            {
                targetErrors.Add("id is required.");
            }
            else if (!seenIds.Add(target.Id))
            {
                targetErrors.Add($"id '{target.Id}' is used more than once.");
            }

            if (!Uri.TryCreate(target.Url, UriKind.Absolute, out var url)
                || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
            {
                targetErrors.Add("url must be an absolute http or https URL.");
            }

            if (target.Match?.Status is not (>= 100 and <= 599))
            {
                targetErrors.Add("match.status must be an HTTP status code between 100 and 599.");
            }

            if (targetErrors.Count > 0)
            {
                errors[key] = [.. targetErrors];
                continue;
            }

            parsed.Add(new FetchTarget(target.Id!, url!, target.Match!.Status!.Value));
        }

        return errors.Count == 0;
    }
}
