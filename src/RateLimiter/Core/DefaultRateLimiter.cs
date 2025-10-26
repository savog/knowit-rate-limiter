using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RateLimiter.Core;

internal sealed class DefaultRateLimiter : IRateLimiter
{
    private readonly RateLimiterOptions _options;
    
    private readonly ConcurrentDictionary<string, Queue<long>> _requests = new(StringComparer.OrdinalIgnoreCase);
    
    private readonly Dictionary<string, (int LimitCount, int LimitPeriod)> _endpointRules = new(StringComparer.OrdinalIgnoreCase);
    
    private readonly ILogger? _logger;

    public DefaultRateLimiter(RateLimiterOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _options.Validate();

        foreach (var e in _options.EndpointLimits)
        {
            var norm = PathUtils.Normalize(e.Endpoint);
            _endpointRules[norm] = (e.RequestLimitCount, e.RequestLimitMs);
        }
    }

    public bool Allow(string ip, string path, DateTime utcNow)
    {
        if (!_options.RequestLimiterEnabled) return true;

        var nowMs = new DateTimeOffset(utcNow).ToUnixTimeMilliseconds();
        var normalizedPath = PathUtils.Normalize(path);

        // Endpoint override
        if (_endpointRules.TryGetValue(normalizedPath, out var rule))
        {
            var endpointKey = MakeKey(ip, normalizedPath);
            return TryHit(endpointKey, rule.LimitPeriod, rule.LimitCount, nowMs);
        }

        // Default
        var generalKey = MakeKey(ip, "*");
        return TryHit(generalKey, _options.DefaultRequestLimitMs, _options.DefaultRequestLimitCount, nowMs);
    }

    private bool TryHit(string key, int limitPeriod, int limitCount, long nowMs)
    {
        var q = _requests.GetOrAdd(key, _ => new Queue<long>());
        
        bool blocked;

        lock (q)
        {
            while (q.Count > 0 && (nowMs - q.Peek()) >= limitPeriod)
            {
                q.Dequeue();
            }

            blocked = q.Count >= limitCount;
            if (!blocked)
            {
                q.Enqueue(nowMs);
            }
        }

        if (!blocked)
        {
            return true;
        }
        
        _logger?.LogDebug("RateLimiter blocked: {Key}", key);
        return false;
    }

    private static string MakeKey(string? ip, string scope)
        => $"{(string.IsNullOrWhiteSpace(ip) ? "unknown" : ip)}|{scope}";
}
