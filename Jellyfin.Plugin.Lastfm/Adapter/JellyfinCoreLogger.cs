using Lastfm.Scrobbler.Core.Interfaces;
using Microsoft.Extensions.Logging;
#pragma warning disable CA1848 // Use LoggerMessage delegates - not applicable to adapter/wrapper classes
#pragma warning disable CA2254 // Template should be a static expression - this is a dynamic logger wrapper
namespace Jellyfin.Plugin.Lastfm.Adapter
{
    public class JellyfinCoreLogger<T> : ICoreLogger<T> where T : class
    {
        private readonly ILogger<T> _logger;

        public JellyfinCoreLogger(ILogger<T> logger)
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
