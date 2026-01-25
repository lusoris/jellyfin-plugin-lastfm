// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Lastfm.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Matches Last.fm tracks to Jellyfin library items.
/// </summary>
public sealed partial class TrackMatcherService : ITrackMatcherService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly LibraryCacheService _cacheService;
    private readonly ILogger<TrackMatcherService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackMatcherService"/> class.
    /// </summary>
    public TrackMatcherService(
        ILibraryManager libraryManager,
        IUserManager userManager,
        LibraryCacheService cacheService,
        ILogger<TrackMatcherService> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Audio?> FindMatchingTrackAsync(string artist, string track, string? musicBrainzId, Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return Task.FromResult<Audio?>(null);
        }

        // Try MusicBrainz ID first (most reliable)
        if (!string.IsNullOrEmpty(musicBrainzId))
        {
            var byMbid = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },
                HasAnyProviderId = new Dictionary<string, string>
                {
                    [MetadataProvider.MusicBrainzRecording.ToString()] = musicBrainzId
                },
                Limit = 1
            }).FirstOrDefault() as Audio;

            if (byMbid != null)
            {
                return Task.FromResult<Audio?>(byMbid);
            }
        }

        // Fallback: Match by artist + track name
        var allTracks = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { BaseItemKind.Audio },
            SearchTerm = track,
            Limit = 100
        });

        foreach (var item in allTracks.OfType<Audio>())
        {
            var itemArtist = item.Artists?.FirstOrDefault() ?? item.AlbumArtists?.FirstOrDefault();
            if (string.Equals(itemArtist, artist, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Name, track, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Audio?>(item);
            }
        }

        LogTrackNotFound(artist, track);
        return Task.FromResult<Audio?>(null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, Audio>> FindMatchingTracksAsync(
        IEnumerable<(string Artist, string Track, string? MusicBrainzId)> tracks,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var trackList = tracks.ToList();
        LogBatchMatchStart(trackList.Count);

        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return new Dictionary<string, Audio>();
        }

        var results = new Dictionary<string, Audio>();

        // First pass: Try MBID lookups in batch
        var mbids = trackList
            .Where(t => !string.IsNullOrEmpty(t.MusicBrainzId))
            .Select(t => t.MusicBrainzId!)
            .Distinct()
            .ToList();

        if (mbids.Any())
        {
            var mbidTracks = await _cacheService.GetTracksByMusicBrainzIdsAsync(mbids, userId, cancellationToken);

            foreach (var track in trackList.Where(t => !string.IsNullOrEmpty(t.MusicBrainzId)))
            {
                var matched = mbidTracks.FirstOrDefault(mt =>
                    mt.GetProviderId(MetadataProvider.MusicBrainzRecording) == track.MusicBrainzId);

                if (matched != null)
                {
                    var key = $"{track.Artist}:{track.Track}";
                    results[key] = matched;
                }
            }
        }

        // Second pass: Name-based matching for remaining tracks
        var unmatchedTracks = trackList.Where(t =>
        {
            var key = $"{t.Artist}:{t.Track}";
            return !results.ContainsKey(key);
        }).ToList();

        if (unmatchedTracks.Any())
        {
            var allTracks = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },
                Recursive = true,
                Limit = 5000 // Reasonable limit for batch operations
            }).OfType<Audio>().ToList();

            foreach (var track in unmatchedTracks)
            {
                var matched = allTracks.FirstOrDefault(item =>
                {
                    var itemArtist = item.Artists?.FirstOrDefault() ?? item.AlbumArtists?.FirstOrDefault();
                    return string.Equals(itemArtist, track.Artist, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(item.Name, track.Track, StringComparison.OrdinalIgnoreCase);
                });

                if (matched != null)
                {
                    var key = $"{track.Artist}:{track.Track}";
                    results[key] = matched;
                }
            }
        }

        LogBatchMatchComplete(results.Count, trackList.Count);
        return results;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found")]
    private partial void LogUserNotFound(Guid userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Track not found in library: {Artist} - {Track}")]
    private partial void LogTrackNotFound(string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Batch matching {Count} tracks")]
    private partial void LogBatchMatchStart(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Batch match: Found {Found} of {Total} tracks")]
    private partial void LogBatchMatchComplete(int found, int total);
}
