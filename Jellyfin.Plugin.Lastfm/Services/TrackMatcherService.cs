// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Text.RegularExpressions;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Matches Jellyfin tracks to Last.fm tracks.
/// </summary>
public sealed partial class TrackMatcherService : ITrackMatcherService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TrackMatcherService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackMatcherService"/> class.
    /// </summary>
    public TrackMatcherService(ILibraryManager libraryManager, ILogger<TrackMatcherService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
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
        // Use provider ID filter directly in query for efficiency
        // Try MusicBrainzRecording first (preferred), then MusicBrainzTrack
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true,
            Limit = 1,
            HasAnyProviderId = new Dictionary<string, string>
            {
                [MetadataProvider.MusicBrainzRecording.ToString()] = mbid
            }
        };

        var result = _libraryManager.GetItemList(query)
            .OfType<Audio>()
            .FirstOrDefault();

        // Fallback to MusicBrainzTrack if not found
        if (result == null)
        {
            query.HasAnyProviderId = new Dictionary<string, string>
            {
                [MetadataProvider.MusicBrainzTrack.ToString()] = mbid
            };
            result = _libraryManager.GetItemList(query)
                .OfType<Audio>()
                .FirstOrDefault();
        }

        return result;
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
}
