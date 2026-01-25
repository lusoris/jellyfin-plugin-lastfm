// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>
/// Service for creating and managing Last.fm-based playlists.
/// </summary>
public class PlaylistService : IPlaylistService
{
    private readonly ILastfmApiClient _lastfmApiClient;
    private readonly IPlaylistManager _playlistManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<PlaylistService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistService"/> class.
    /// </summary>
    public PlaylistService(
        ILastfmApiClient lastfmApiClient,
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILogger<PlaylistService> logger)
    {
        _lastfmApiClient = lastfmApiClient;
        _playlistManager = playlistManager;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PlaylistResult> CreateSimilarArtistsPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return PlaylistResult.FailureResult("User not found");
        }

        var lastfmUser = Plugin.Instance?.Configuration.GetUserConfig(userId);
        if (lastfmUser == null || string.IsNullOrEmpty(lastfmUser.Username))
        {
            return PlaylistResult.FailureResult("User not connected to Last.fm");
        }

        _logger.LogInformation("Creating similar artists playlist for {Username}", lastfmUser.Username);

        // Get user's top artists from Last.fm
        var topArtistsResponse = await _lastfmApiClient.GetTopTracksAsync(
            lastfmUser.Username,
            "3month",
            limit: 10,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (topArtistsResponse?.TopTracks?.Tracks == null || topArtistsResponse.TopTracks.Tracks.Count == 0)
        {
            return PlaylistResult.FailureResult("No recent listening history found");
        }

        // Get unique artists from top tracks
        var topArtists = topArtistsResponse.TopTracks.Tracks
            .Where(t => t.Artist != null)
            .Select(t => t.Artist!.Name)
            .Distinct()
            .Take(5)
            .ToList();

        var trackIds = new List<Guid>();

        foreach (var artistName in topArtists)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Get similar artists from Last.fm
            var similarResponse = await _lastfmApiClient.GetSimilarArtistsAsync(
                artistName,
                limit: 10,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (similarResponse?.SimilarArtists?.Artists == null)
            {
                continue;
            }

            // Find tracks from similar artists in Jellyfin library
            foreach (var similarArtist in similarResponse.SimilarArtists.Artists.Take(5))
            {
                var localTracks = FindTracksByArtist(similarArtist.Name, userId);
                foreach (var track in localTracks.Take(3))
                {
                    if (!trackIds.Contains(track.Id) && trackIds.Count < maxTracks)
                    {
                        trackIds.Add(track.Id);
                    }
                }
            }
        }

        if (trackIds.Count == 0)
        {
            return PlaylistResult.FailureResult("No matching tracks found in library");
        }

        // Create the playlist
        return await CreatePlaylistAsync(userId, playlistName, trackIds).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PlaylistResult> CreateSimilarTracksPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return PlaylistResult.FailureResult("User not found");
        }

        var lastfmUser = Plugin.Instance?.Configuration.GetUserConfig(userId);
        if (lastfmUser == null || string.IsNullOrEmpty(lastfmUser.Username))
        {
            return PlaylistResult.FailureResult("User not connected to Last.fm");
        }

        _logger.LogInformation("Creating similar tracks playlist for {Username}", lastfmUser.Username);

        // Get user's loved tracks from Last.fm
        var lovedTracksResponse = await _lastfmApiClient.GetLovedTracksAsync(
            lastfmUser.Username,
            limit: 20,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (lovedTracksResponse?.LovedTracks?.Tracks == null || lovedTracksResponse.LovedTracks.Tracks.Count == 0)
        {
            return PlaylistResult.FailureResult("No loved tracks found");
        }

        var trackIds = new List<Guid>();

        // Get similar tracks for each loved track
        foreach (var lovedTrack in lovedTracksResponse.LovedTracks.Tracks.Take(10))
        {
            if (cancellationToken.IsCancellationRequested || trackIds.Count >= maxTracks)
            {
                break;
            }

            if (lovedTrack.Artist == null)
            {
                continue;
            }

            var similarResponse = await _lastfmApiClient.GetSimilarTracksAsync(
                lovedTrack.Artist.Name,
                lovedTrack.Name,
                limit: 10,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (similarResponse?.SimilarTracks?.Tracks == null)
            {
                continue;
            }

            // Find matching tracks in Jellyfin library
            foreach (var similarTrack in similarResponse.SimilarTracks.Tracks.Take(5))
            {
                if (similarTrack.Artist == null)
                {
                    continue;
                }

                var localTrack = FindTrack(similarTrack.Artist.Name, similarTrack.Name, userId);
                if (localTrack != null && !trackIds.Contains(localTrack.Id) && trackIds.Count < maxTracks)
                {
                    trackIds.Add(localTrack.Id);
                }
            }
        }

        if (trackIds.Count == 0)
        {
            return PlaylistResult.FailureResult("No matching tracks found in library");
        }

        return await CreatePlaylistAsync(userId, playlistName, trackIds).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PlaylistResult> CreateRediscoverFavoritesPlaylistAsync(
        Guid userId,
        string playlistName,
        int maxTracks = 50,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return PlaylistResult.FailureResult("User not found");
        }

        var lastfmUser = Plugin.Instance?.Configuration.GetUserConfig(userId);
        if (lastfmUser == null || string.IsNullOrEmpty(lastfmUser.Username))
        {
            return PlaylistResult.FailureResult("User not connected to Last.fm");
        }

        _logger.LogInformation("Creating rediscover favorites playlist for {Username}", lastfmUser.Username);

        // Get user's loved tracks
        var lovedTracksResponse = await _lastfmApiClient.GetLovedTracksAsync(
            lastfmUser.Username,
            limit: 200,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (lovedTracksResponse?.LovedTracks?.Tracks == null || lovedTracksResponse.LovedTracks.Tracks.Count == 0)
        {
            return PlaylistResult.FailureResult("No loved tracks found");
        }

        var trackIds = new List<Guid>();

        // Find loved tracks that exist in library and haven't been played recently
        foreach (var lovedTrack in lovedTracksResponse.LovedTracks.Tracks)
        {
            if (lovedTrack.Artist == null || trackIds.Count >= maxTracks)
            {
                continue;
            }

            var localTrack = FindTrack(lovedTrack.Artist.Name, lovedTrack.Name, userId);
            if (localTrack == null)
            {
                continue;
            }

            // Check if track hasn't been played recently (within last 30 days)
            var userData = _userDataManager.GetUserData(user, localTrack);
            if (userData?.LastPlayedDate == null ||
                userData.LastPlayedDate.Value < DateTime.UtcNow.AddDays(-30))
            {
                trackIds.Add(localTrack.Id);
            }
        }

        if (trackIds.Count == 0)
        {
            return PlaylistResult.FailureResult("No forgotten favorites found in library");
        }

        // Shuffle the tracks
        var random = new Random();
        trackIds = trackIds.OrderBy(_ => random.Next()).Take(maxTracks).ToList();

        return await CreatePlaylistAsync(userId, playlistName, trackIds).ConfigureAwait(false);
    }

    private async Task<PlaylistResult> CreatePlaylistAsync(Guid userId, string playlistName, List<Guid> trackIds)
    {
        try
        {
            var request = new PlaylistCreationRequest
            {
                Name = playlistName,
                ItemIdList = trackIds,
                UserId = userId,
                MediaType = MediaType.Audio,
                Users = []
            };

            var result = await _playlistManager.CreatePlaylist(request).ConfigureAwait(false);

            _logger.LogInformation(
                "Created playlist '{PlaylistName}' with {TrackCount} tracks",
                playlistName,
                trackIds.Count);

            var playlistId = Guid.Parse(result.Id);
            return PlaylistResult.SuccessResult(playlistId, playlistName, trackIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create playlist '{PlaylistName}'", playlistName);
            return PlaylistResult.FailureResult($"Failed to create playlist: {ex.Message}");
        }
    }

    private IEnumerable<Audio> FindTracksByArtist(string artistName, Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return [];
        }

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            ArtistIds = GetArtistId(artistName),
            Recursive = true,
            Limit = 20
        };

        return _libraryManager.GetItemList(query)
            .OfType<Audio>();
    }

    private Audio? FindTrack(string artistName, string trackName, Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return null;
        }

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            SearchTerm = trackName,
            Recursive = true,
            Limit = 50
        };

        var results = _libraryManager.GetItemList(query).OfType<Audio>();

        // Find best match by artist and track name
        return results.FirstOrDefault(t =>
            string.Equals(t.Name, trackName, StringComparison.OrdinalIgnoreCase) &&
            t.Artists.Any(a => string.Equals(a, artistName, StringComparison.OrdinalIgnoreCase)));
    }

    private Guid[] GetArtistId(string artistName)
    {
        try
        {
            var artist = _libraryManager.GetArtist(artistName, new MediaBrowser.Controller.Dto.DtoOptions());
            return artist != null ? [artist.Id] : [];
        }
        catch
        {
            return [];
        }
    }
}
