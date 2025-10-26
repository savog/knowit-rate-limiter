namespace RateLimiter.Core;

internal static class PathUtils
{
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        // Strip query string or fragment
        var queryOrFragmentIndex = path.IndexOfAny(['?', '#']);
        var normalized = queryOrFragmentIndex >= 0 ? path[..queryOrFragmentIndex] : path;

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        // Remove trailing slash (except for root)
        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized.ToLowerInvariant();
    }
}
