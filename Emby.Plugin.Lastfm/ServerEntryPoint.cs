// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System;
using System.Linq;
using System.Threading.Tasks;
using Emby.Plugin.Lastfm.Configuration;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;

namespace Emby.Plugin.Lastfm;

/// <summary>
/// Entry point for the Emby plugin.
/// Subscribes to playback events and handles scrobbling logic.
/// </summary>
public sealed class ServerEntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private readonly ILastfmApiClient _lastfmApiClient;
    private readonly IScrobbleQueue<Scrobble, string> _scrobbleQueue;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
    /// </summary>
    public ServerEntryPoint(
        ISessionManager sessionManager,
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        IUserManager userManager,
        ILogManager logManager,
        ILastfmApiClient lastfmApiClient,
        IScrobbleQueue<Scrobble, string> scrobbleQueue)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logManager.GetLogger(GetType().Name);
        _lastfmApiClient = lastfmApiClient;
        _scrobbleQueue = scrobbleQueue;
    }

    /// <summary>
    /// Called when the server starts. Subscribe to events here.
    /// </summary>
    public void Run()
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _userDataManager.UserDataSaved += OnUserDataSaved;

        _logger.Info("Last.fm plugin started");
    }

    /// <summary>
    /// Disposes resources and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _userDataManager.UserDataSaved -= OnUserDataSaved;

        _logger.Info("Last.fm plugin stopped");
    }

    private void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
    {
        Task.Run(() => HandlePlaybackStartAsync(e)).ConfigureAwait(false);
    }

    private void OnPlaybackStopped(object sender, PlaybackProgressEventArgs e)
    {
        Task.Run(() => HandlePlaybackStoppedAsync(e)).ConfigureAwait(false);
    }

    private void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
    {
        Task.Run(() => HandleUserDataSavedAsync(e)).ConfigureAwait(false);
    }

    private async Task HandlePlaybackStartAsync(PlaybackProgressEventArgs e)
    {
        try
        {
            // Type check: Only handle Audio items
            if (e.Item == null || e.Item.GetType().Name != "Audio")
            {
                return;
            }

            var audio = e.Item as Audio;
            if (audio == null)
            {
                return;
            }

            var artist = audio.Artists != null && audio.Artists.Length > 0 ? audio.Artists[0] : "Unknown";
            _logger.Debug("Playback started: {0} - {1}", artist, audio.Name);

            // Get Last.fm users for this Emby user
            var userId = e.Session?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var lastfmUsers = GetLastfmUsersForEmbyUser(userId!);
            if (lastfmUsers.Length == 0)
            {
                return;
            }

            // Create scrobble info for Now Playing
            var scrobble = CreateTrackFromAudio(audio);

            // Update Now Playing for all configured Last.fm users
            foreach (var lastfmUser in lastfmUsers)
            {
                try
                {
                    await _lastfmApiClient.NowPlaying(scrobble, lastfmUser).ConfigureAwait(false);
                    _logger.Debug("Now Playing updated for user: {0}", lastfmUser.Username);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException($"Error updating Now Playing for user {lastfmUser.Username}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error handling playback start", ex);
        }
    }

    private async Task HandlePlaybackStoppedAsync(PlaybackProgressEventArgs e)
    {
        try
        {
            // Type check: Only handle Audio items
            if (e.Item == null || e.Item.GetType().Name != "Audio")
            {
                return;
            }

            var audio = e.Item as Audio;
            if (audio == null)
            {
                return;
            }

            // Check scrobble eligibility
            if (!IsScrobbleEligible(audio.RunTimeTicks ?? 0, e.PlaybackPositionTicks ?? 0))
            {
                var artist = audio.Artists != null && audio.Artists.Length > 0 ? audio.Artists[0] : "Unknown";
                _logger.Debug("Track not eligible for scrobbling: {0} - {1}", artist, audio.Name);
                return;
            }

            var scrobbleArtist = audio.Artists != null && audio.Artists.Length > 0 ? audio.Artists[0] : "Unknown";
            _logger.Info("Scrobbling: {0} - {1}", scrobbleArtist, audio.Name);

            // Get Last.fm users for this Emby user
            var userId = e.Session?.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var lastfmUsers = GetLastfmUsersForEmbyUser(userId!);
            if (lastfmUsers.Length == 0)
            {
                return;
            }

            // Create scrobble and add to queue
            var scrobble = CreateScrobbleFromAudio(audio);

            foreach (var lastfmUser in lastfmUsers)
            {
                _scrobbleQueue.Enqueue(scrobble, lastfmUser.SessionKey);
                _logger.Debug("Queued scrobble for user: {0}", lastfmUser.Username);
            }

            await Task.CompletedTask; // Satisfy async warning
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error handling playback stopped", ex);
        }
    }

    private async Task HandleUserDataSavedAsync(UserDataSaveEventArgs e)
    {
        try
        {
            // Check if this is a favorite change for an Audio item
            if (e.SaveReason != UserDataSaveReason.UpdateUserRating)
            {
                return;
            }

            var item = _libraryManager.GetItemById(e.Item.Id);
            if (item == null || item.GetType().Name != "Audio")
            {
                return;
            }

            var audio = item as Audio;
            if (audio == null)
            {
                return;
            }

            var userData = _userDataManager.GetUserData(e.User, e.Item);
            var artist = audio.Artists != null && audio.Artists.Length > 0 ? audio.Artists[0] : "Unknown";
            _logger.Debug("Favorite changed: {0} - {1} -> {2}", artist, audio.Name, userData.IsFavorite);

            // Get Last.fm users for this Emby user
            var lastfmUsers = GetLastfmUsersForEmbyUser(e.User.Id.ToString("N"));
            if (lastfmUsers.Length == 0)
            {
                return;
            }

            var trackArtist = audio.Artists != null && audio.Artists.Length > 0 ? audio.Artists[0] : null;
            if (string.IsNullOrEmpty(trackArtist) || string.IsNullOrEmpty(audio.Name))
            {
                _logger.Warn("Cannot love/unlove track: missing artist or track name");
                return;
            }

            var track = CreateTrackFromAudio(audio);

            // Love or unlove for all configured Last.fm users
            foreach (var lastfmUser in lastfmUsers)
            {
                try
                {
                    await _lastfmApiClient.LoveTrack(track, lastfmUser, userData.IsFavorite).ConfigureAwait(false);
                    var action = userData.IsFavorite ? "Loved" : "Unloved";
                    _logger.Info("{0} track for user {1}: {2} - {3}", action, lastfmUser.Username, trackArtist, audio.Name);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException($"Error loving/unloving track for user {lastfmUser.Username}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error handling user data saved", ex);
        }
    }

    /// <summary>
    /// Determines if a track is eligible for scrobbling based on Last.fm rules.
    /// </summary>
    private static bool IsScrobbleEligible(long trackLengthTicks, long playedTicks)
    {
        const long minimumSongLength = 30 * TimeSpan.TicksPerSecond; // 30 seconds
        const long minimumPlayTime = 4 * TimeSpan.TicksPerMinute;   // 4 minutes
        const double minimumPlayPercentage = 50.0;

        // Track must be at least 30 seconds long
        if (trackLengthTicks < minimumSongLength)
        {
            return false;
        }

        // Calculate play percentage
        var playPercent = ((double)playedTicks / trackLengthTicks) * 100;

        // Must have played 50% OR 4 minutes (whichever occurs earlier)
        return playPercent >= minimumPlayPercentage || playedTicks >= minimumPlayTime;
    }

    /// <summary>
    /// Gets the configured Last.fm users for a given Emby user ID.
    /// </summary>
    private LastfmUser[] GetLastfmUsersForEmbyUser(string embyUserId)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.LastfmUsers == null || config.LastfmUsers.Length == 0)
        {
#pragma warning disable CA1825
            return new LastfmUser[0];
#pragma warning restore CA1825
        }

        var embyUser = _userManager.GetUserById(embyUserId);
        if (embyUser == null)
        {
#pragma warning disable CA1825
            return new LastfmUser[0];
#pragma warning restore CA1825
        }

        // Match by Emby username (stored in JellyfinUserId as string for compatibility)
        return config.LastfmUsers
            .Where(u => u.JellyfinUserId.ToString().Equals(embyUser.Name, StringComparison.OrdinalIgnoreCase) ||
                       u.JellyfinUserId.ToString().Equals(embyUserId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Creates a Track object from an Audio entity.
    /// </summary>
    private static Track CreateTrackFromAudio(Audio audio)
    {
        return new Track
        {
            Id = audio.Id,
            Name = audio.Name,
            Artists = audio.Artists?.ToList(),
            Album = audio.Album,
            AlbumArtists = audio.AlbumArtists?.ToList(),
            RunTimeTicks = audio.RunTimeTicks,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProviderIds = new System.Collections.Generic.Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Creates a Scrobble object from an Audio entity.
    /// </summary>
    private static Scrobble CreateScrobbleFromAudio(Audio audio)
    {
        var artist = audio.Artists != null && audio.Artists.Length > 0 ? audio.Artists[0] : "Unknown Artist";
        var duration = audio.RunTimeTicks.HasValue ? (int)(audio.RunTimeTicks.Value / TimeSpan.TicksPerSecond) : 0;

        var scrobble = new Scrobble(audio.Name ?? "Unknown Track", artist, DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            Album = audio.Album,
            AlbumArtist = audio.AlbumArtists != null && audio.AlbumArtists.Length > 0 ? audio.AlbumArtists[0] : null,
            Duration = duration > 0 ? duration : null
        };

        return scrobble;
    }
}
