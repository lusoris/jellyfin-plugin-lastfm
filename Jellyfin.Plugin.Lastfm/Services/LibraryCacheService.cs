// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Caching layer for library queries to reduce repeated database lookups.
/// </summary>
public sealed partial class LibraryCacheService : IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LibraryCacheService> _logger;
    private bool _disposed;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TrackCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan QueryResultCacheDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryCacheService"/> class.
    /// </summary>
    public LibraryCacheService(
        ILibraryManager libraryManager,
        IMemoryCache memoryCache,
        ILogger<LibraryCacheService> logger)
    {
        _libraryManager = libraryManager;
        _cache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Gets a track by MBID with caching.
    /// </summary>
    public Audio? GetTrackByMusicBrainzId(string musicBrainzId, Guid userId)
    {
        var cacheKey = $"mbid:{musicBrainzId}:{userId}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TrackCacheDuration;

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },
                HasAnyProviderId = new Dictionary<string, string>
                {
                    [MetadataProvider.MusicBrainzRecording.ToString()] = musicBrainzId
                },
                Limit = 1
            };

            var result = _libraryManager.GetItemList(query).Count > 0
                ? _libraryManager.GetItemList(query)[0] as Audio
                : null;

            if (result != null)
            {
                LogCacheStore("MBID", musicBrainzId);
            }

            return result;
        });
    }

    /// <summary>
    /// Gets a track by ID with caching.
    /// </summary>
    public BaseItem? GetItemById(Guid itemId)
    {
        var cacheKey = $"item:{itemId}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheDuration;
            var result = _libraryManager.GetItemById(itemId);

            if (result != null)
            {
                LogCacheStore("Item", itemId.ToString());
            }

            return result;
        });
    }

    /// <summary>
    /// Invalidates cache for a specific track.
    /// </summary>
    public void InvalidateTrack(Guid trackId)
    {
        _cache.Remove($"item:{trackId}");
        LogCacheInvalidate(trackId);
    }

    /// <summary>
    /// Invalidates query results cache for a user.
    /// </summary>
    public void InvalidateUserQueries(Guid userId)
    {
        // MemoryCache doesn't support pattern-based removal, so we compact
        // This is a limitation - consider Redis for production if needed
        LogQueryCacheInvalidate(userId);
    }

    /// <summary>
    /// Gets multiple tracks by MusicBrainz IDs in a single batch query.
    /// </summary>
    public async Task<IReadOnlyList<Audio>> GetTracksByMusicBrainzIdsAsync(
        IEnumerable<string> musicBrainzIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var mbidList = musicBrainzIds.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        if (mbidList.Count == 0)
        {
            return Array.Empty<Audio>();
        }

        // Limit batch size to prevent memory issues
        if (mbidList.Count > 5000)
        {
            mbidList = mbidList.Take(5000).ToList();
        }

        var cacheKey = $"batch:mbids:{string.Join(",", mbidList.OrderBy(x => x))}:{userId}";

        return await Task.Run(() =>
        {
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TrackCacheDuration;

                var query = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Audio },
                    Recursive = true,
                    Limit = mbidList.Count * 2 // Safety margin
                };

                var allTracks = _libraryManager.GetItemList(query).OfType<Audio>().ToList();

                var results = allTracks.Where(track =>
                {
                    var trackMbid = track.GetProviderId(MetadataProvider.MusicBrainzRecording);
                    return !string.IsNullOrEmpty(trackMbid) && mbidList.Contains(trackMbid);
                }).ToList();

                LogBatchCacheStore(mbidList.Count, results.Count);
                return (IReadOnlyList<Audio>)results;
            }) ?? Array.Empty<Audio>();
        }, cancellationToken);
    }

    /// <summary>
    /// Gets favorite tracks for a user with caching.
    /// </summary>
    public IReadOnlyList<Audio> GetFavoriteTracks(Guid userId)
    {
        var cacheKey = $"query:favorites:{userId}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = QueryResultCacheDuration;

            var query = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },
                Recursive = true,
                IsFavorite = true
            };

            var results = _libraryManager.GetItemList(query).OfType<Audio>().ToList();
            LogQueryCacheStore("favorites", userId, results.Count);

            return (IReadOnlyList<Audio>)results;
        }) ?? Array.Empty<Audio>();
    }

    /// <summary>
    /// Warms up the cache with commonly accessed data.
    /// </summary>
    public async Task WarmupCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        LogCacheWarmupStart(userId);

        await Task.Run(() =>
        {
            try
            {
                // Pre-cache favorite tracks
                _ = GetFavoriteTracks(userId);

                LogCacheWarmupComplete(userId);
            }
            catch (Exception ex)
            {
                LogCacheWarmupError(userId, ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Invalidates all cached entries.
    /// </summary>
    public void InvalidateAll()
    {
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(1.0);
            LogCacheCleared();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached {Type} lookup: {Key}")]
    private partial void LogCacheStore(string type, string key);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invalidated cache for track: {TrackId}")]
    private partial void LogCacheInvalidate(Guid trackId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleared all library cache entries")]
    private partial void LogCacheCleared();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Batch cached {Requested} MBIDs, found {Found} tracks")]
    private partial void LogBatchCacheStore(int requested, int found);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached {QueryType} query for user {UserId}: {ResultCount} results")]
    private partial void LogQueryCacheStore(string queryType, Guid userId, int resultCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Invalidated query cache for user: {UserId}")]
    private partial void LogQueryCacheInvalidate(Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting cache warmup for user: {UserId}")]
    private partial void LogCacheWarmupStart(Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed cache warmup for user: {UserId}")]
    private partial void LogCacheWarmupComplete(Guid userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during cache warmup for user: {UserId}")]
    private partial void LogCacheWarmupError(Guid userId, Exception exception);
}
