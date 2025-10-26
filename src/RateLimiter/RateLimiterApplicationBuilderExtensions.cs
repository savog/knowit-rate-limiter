using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RateLimiter.Core;

namespace RateLimiter
{
    /// <summary>
    /// Extension methods for adding <see cref="RateLimiterMiddleware"/> to the ASP.NET Core pipeline.
    /// </summary>
    public static class RateLimiterApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers <see cref="RateLimiterMiddleware"/> using the specified <paramref name="options"/>.
        /// </summary>
        public static IApplicationBuilder UseRateLimiter(
            this IApplicationBuilder app,
            RateLimiterOptions options)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(options);

            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger(nameof(RateLimiterMiddleware));

            IRateLimiter core = new DefaultRateLimiter(options, logger);

            return app.Use(RateLimiterMiddleware.Create(core, logger));
        }

        /// <summary>
        /// Registers <see cref="RateLimiterMiddleware"/> using an inline configuration delegate.
        /// </summary>
        public static IApplicationBuilder UseRateLimiter(
            this IApplicationBuilder app,
            Action<RateLimiterOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new RateLimiterOptions();
            configure(options);
            return app.UseRateLimiter(options);
        }

        /// <summary>
        /// Registers <see cref="RateLimiterMiddleware"/> using configuration from <paramref name="configurationSection"/>.
        /// </summary>
        public static IApplicationBuilder UseRateLimiter(
            this IApplicationBuilder app,
            IConfigurationSection configurationSection)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(configurationSection);

            var options = new RateLimiterOptions();
            configurationSection.Bind(options);
            return app.UseRateLimiter(options);
        }
    }
}
