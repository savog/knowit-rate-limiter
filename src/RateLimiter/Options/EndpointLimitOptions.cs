namespace RateLimiter;

/// <summary>
/// Defines rate limit settings for a specific API endpoint.
/// </summary>
public sealed class EndpointLimitOptions
{
    /// <summary>
    /// The endpoint path or route pattern this rule applies to.
    /// </summary>
    /// <example>
    /// "/api/users" or "/api/orders/{id}"
    /// </example>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The time window, in milliseconds, for this rule.
    /// </summary>
    /// <example>
    /// 5000 means requests are counted within 5 seconds.
    /// </example>
    public int RequestLimitMs { get; set; }

    /// <summary>
    /// The maximum number of allowed requests within the defined time window.
    /// </summary>
    /// <example>
    /// 20 with <see cref="RequestLimitMs"/> = 10000 allows 20 requests per 10 seconds.
    /// </example>
    public int RequestLimitCount { get; set; }
}
