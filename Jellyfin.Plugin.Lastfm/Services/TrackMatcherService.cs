// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Matches Jellyfin tracks to Last.fm tracks.
/// </summary>
public sealed partial class TrackMatcherService : ITrackMatcherService, IDisposable
{
    /// <summary>
    /// Cache duration for MusicBrainz ID lookups.
    /// </summary>
    private static readonly TimeSpan MbidCacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Cache duration for negative (not found) lookups - shorter to allow retries.
    /// </summary>
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromMinutes(5);

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TrackMatcherService> _logger;
    private readonly MemoryCache _mbidCache;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackMatcherService"/> class.
    /// </summary>
    public TrackMatcherService(ILibraryManager libraryManager, ILogger<TrackMatcherService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _mbidCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 10000 // Max 10k cached lookups
        });
    }

    /// <inheritdoc />
    public Task<Audio?> FindMatchingTrackAsync(string lastfmArtist, string lastfmTrack, string? lastfmMbid, Guid userId)
    {
        // Strategy 1: Try MusicBrainz ID if available
        if (!string.IsNullOrEmpty(lastfmMbid))
        {
            var mbidMatch = FindByMusicBrainzId(lastfmMbid);
            if (mbidMatch != null)
            {
                LogFoundMbidMatch(lastfmArtist, lastfmTrack);
                return Task.FromResult<Audio?>(mbidMatch);
            }
        }

        // Strategy 2: Exact artist + track name match
        var exactMatch = FindByExactName(lastfmArtist, lastfmTrack);
        if (exactMatch != null)
        {
            LogFoundExactMatch(lastfmArtist, lastfmTrack);
            return Task.FromResult<Audio?>(exactMatch);
        }

        // Strategy 3: Fuzzy artist + track name match
        var fuzzyMatch = FindByFuzzyName(lastfmArtist, lastfmTrack);
        if (fuzzyMatch != null)
        {
            LogFoundFuzzyMatch(lastfmArtist, lastfmTrack, fuzzyMatch.Artists.FirstOrDefault(), fuzzyMatch.Name);
            return Task.FromResult<Audio?>(fuzzyMatch);
        }

        LogNoMatchFound(lastfmArtist, lastfmTrack);
        return Task.FromResult<Audio?>(null);
    }

    private Audio? FindByMusicBrainzId(string mbid)
    {
        // Check cache first
        if (_mbidCache.TryGetValue(mbid, out Guid cachedItemId))
        {
            // Negative cache hit (not found marker)
            if (cachedItemId == Guid.Empty)
            {
                return null;
            }

            // Positive cache hit - retrieve the item
            var cachedItem = _libraryManager.GetItemById(cachedItemId) as Audio;
            if (cachedItem != null)
            {
                return cachedItem;
            }

            // Item was deleted - remove from cache
            _mbidCache.Remove(mbid);
        }

        // Use provider ID filter directly in query for efficiency
        // Try MusicBrainzRecording first (preferred), then MusicBrainzTrack
        var result = QueryByProviderId(MetadataProvider.MusicBrainzRecording.ToString(), mbid)
            ?? QueryByProviderId(MetadataProvider.MusicBrainzTrack.ToString(), mbid);

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = result != null ? MbidCacheDuration : NegativeCacheDuration
        };

        _mbidCache.Set(mbid, result?.Id ?? Guid.Empty, cacheOptions);

        return result;
    }

    private Audio? QueryByProviderId(string providerName, string mbid)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true,
            Limit = 1,
            HasAnyProviderId = new Dictionary<string, string>
            {
                [providerName] = mbid
            }
        };

        return _libraryManager.GetItemList(query)
            .OfType<Audio>()
            .FirstOrDefault();
    }

    private Audio? FindByExactName(string artist, string track)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true,
            SearchTerm = track
        };

        var items = _libraryManager.GetItemList(query);

        return items
            .OfType<Audio>()
            .FirstOrDefault(a =>
                string.Equals(a.Name, track, StringComparison.OrdinalIgnoreCase) &&
                a.Artists.Any(ar => string.Equals(ar, artist, StringComparison.OrdinalIgnoreCase)));
    }

    private Audio? FindByFuzzyName(string artist, string track)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true
        };

        var items = _libraryManager.GetItemList(query);

        return items
            .OfType<Audio>()
            .FirstOrDefault(a =>
                IsLike(a.Name, track) &&
                a.Artists.Any(ar => IsLike(ar, artist)));
    }

    /// <inheritdoc />
    public bool IsLike(string source, string target)
    {
        var normalizedSource = NormalizeString(source);
        var normalizedTarget = NormalizeString(target);

        return string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a string for comparison by removing special characters and whitespace.
    /// </summary>
    private static string NormalizeString(string input)
    {
        // Remove special characters, keep only alphanumeric
        var cleaned = SpecialCharsRegex().Replace(input, string.Empty);
        // Remove all whitespace
        return WhitespaceRegex().Replace(cleaned, string.Empty);
    }

    [GeneratedRegex(@"[\~#%&*{}/:<>?,\.\-\(\)\|""\']")]
    private static partial Regex SpecialCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found match by MusicBrainz ID: {Artist} - {Track}")]
    private partial void LogFoundMbidMatch(string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found exact match: {Artist} - {Track}")]
    private partial void LogFoundExactMatch(string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found fuzzy match: {Artist} - {Track} => {MatchArtist} - {MatchTrack}")]
    private partial void LogFoundFuzzyMatch(string artist, string track, string? matchArtist, string? matchTrack);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No match found for: {Artist} - {Track}")]
    private partial void LogNoMatchFound(string artist, string track);

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _mbidCache.Dispose();
            _disposed = true;
        }
    }
}
