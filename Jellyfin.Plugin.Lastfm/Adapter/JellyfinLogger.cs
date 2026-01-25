using Lastfm.Scrobbler.Core.Interfaces;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1848 // Use LoggerMessage delegates - not applicable to adapter/wrapper classes
#pragma warning disable CA2254 // Template should be a static expression - this is a dynamic logger wrapper

namespace Jellyfin.Plugin.Lastfm.Adapter
{
    /// <summary>
    /// An adapter to bridge the core logger with Jellyfin's logging framework.
    /// </summary>
    /// <typeparam name="T">The type to log for.</typeparam>
    public class JellyfinLogger<T> : ICoreLogger<T> where T : class
    {
        private readonly ILogger<T> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinLogger{T}"/> class.
        /// </summary>
        /// <param name="logger">The Jellyfin logger instance.</param>
        public JellyfinLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }
    }
}
