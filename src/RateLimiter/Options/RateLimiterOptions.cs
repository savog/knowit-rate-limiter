using RateLimiter.Core;

namespace RateLimiter
{
    /// <summary>
    /// Represents configuration options for the <see cref="RateLimiter"/> middleware.
    /// These options define the default request limits, enable/disable behavior,
    /// and allow fine-grained per-endpoint rate limiting rules.
    /// </summary>
    /// <remarks>
    /// All properties must be explicitly configured by the consumer.
    /// No default values are assumed. This design ensures explicit configuration
    /// and avoids silent misconfigurations.
    /// </remarks>
    public sealed class RateLimiterOptions
    {
        /// <summary>
        /// Enables or disables the rate limiter entirely.
        /// When set to <c>false</c>, all other properties are ignored.
        /// </summary>
        public bool RequestLimiterEnabled { get; set; }

        /// <summary>
        /// Defines the default time period (in milliseconds)
        /// for the rate-limiting calculation across all endpoints.
        /// </summary>
        /// <example>
        /// For example, if set to 1000, each client will be limited
        /// by the number of allowed requests within one second.
        /// </example>
        public int DefaultRequestLimitMs { get; set; }

        /// <summary>
        /// Defines the default maximum number of requests allowed
        /// within the specified <see cref="DefaultRequestLimitMs"/> time period.
        /// </summary>
        /// <example>
        /// For example, if <see cref="DefaultRequestLimitMs"/> = 1000 and
        /// this property = 10, then a client can make up to 10 requests per second.
        /// </example>
        public int DefaultRequestLimitCount { get; set; }

        /// <summary>
        /// A collection of fine-grained rate limit rules, defined per endpoint.
        /// Each rule overrides the default rate limit values for its target path.
        /// </summary>
        /// <remarks>
        /// If this list is empty, only the default rate limit settings apply.
        /// </remarks>
        public List<EndpointLimitOptions> EndpointLimits { get; set; } = new();

        /// <summary>
        /// Validates the current configuration for logical consistency.
        /// Throws descriptive exceptions if configuration is invalid.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when any time period or request count is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when endpoint definitions are missing or duplicated.
        /// </exception>
        /// <remarks>
        /// This method is typically invoked internally by the rate limiter core
        /// during initialization. Consumers can also call it manually for diagnostics.
        /// </remarks>
        public void Validate()
        {
            // If limiter is disabled, skip validation entirely.
            if (!RequestLimiterEnabled) return;

            if (DefaultRequestLimitMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(DefaultRequestLimitMs), "Must be greater than zero.");

            if (DefaultRequestLimitCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(DefaultRequestLimitCount), "Must be greater than zero.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in EndpointLimits)
            {
                if (string.IsNullOrWhiteSpace(e.Endpoint))
                    throw new ArgumentException("Endpoint cannot be null or empty.", nameof(EndpointLimits));

                var normalized = PathUtils.Normalize(e.Endpoint);
                if (!seen.Add(normalized))
                    throw new ArgumentException($"Duplicate endpoint rule detected: '{normalized}'.",
                        nameof(EndpointLimits));

                if (e.RequestLimitMs <= 0)
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(EndpointLimitOptions.RequestLimitMs)} must be greater than zero.");

                if (e.RequestLimitCount <= 0)
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(EndpointLimitOptions.RequestLimitCount)} must be greater than zero.");
            }
        }
    }
}
