// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>
/// Validates and manages scrobbles according to Last.fm rules.
/// </summary>
public class ScrobbleService : IScrobbleService, IDisposable
{
    /// <summary>
    /// Minimum track length to be eligible for scrobbling (30 seconds).
    /// </summary>
    public static readonly TimeSpan MinimumTrackLength = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Minimum play time to trigger a scrobble (4 minutes).
    /// </summary>
    public static readonly TimeSpan MinimumPlayTime = TimeSpan.FromMinutes(4);

    /// <summary>
    /// Minimum play percentage to trigger a scrobble (50%).
    /// </summary>
    public const double MinimumPlayPercentage = 50.0;

    /// <summary>
    /// Default time window for duplicate detection.
    /// </summary>
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(15);

    private readonly ILogger<ScrobbleService> _logger;
    private readonly MemoryCache _scrobbleCache;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrobbleService"/> class.
    /// </summary>
    public ScrobbleService(ILogger<ScrobbleService> logger)
    {
        _logger = logger;
        _scrobbleCache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <inheritdoc />
    public bool IsScrobbleEligible(long trackLengthTicks, long playedTicks)
    {
        // Track must be at least 30 seconds long
        if (trackLengthTicks < MinimumTrackLength.Ticks)
        {
            return false;
        }

        // Must have played 50% OR 4 minutes (whichever comes first)
        var playPercent = (double)playedTicks / trackLengthTicks * 100;
        return playPercent >= MinimumPlayPercentage || playedTicks >= MinimumPlayTime.Ticks;
    }

    /// <inheritdoc />
    public bool IsDuplicateScrobble(Guid userId, Guid trackId)
    {
        var cacheKey = $"{userId}:{trackId}";
        return _scrobbleCache.TryGetValue(cacheKey, out _);
    }

    /// <inheritdoc />
    public void RecordScrobble(Guid userId, Guid trackId)
    {
        var cacheKey = $"{userId}:{trackId}";
        _scrobbleCache.Set(cacheKey, true, DuplicateWindow);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _scrobbleCache.Dispose();
            }

            _disposed = true;
        }
    }
}
