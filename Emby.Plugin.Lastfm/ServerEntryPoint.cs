// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System;
using System.Linq;
using System.Threading.Tasks;
using Emby.Plugin.Lastfm.Configuration;
using Lastfm.Scrobbler.Core.Interfaces;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
    /// </summary>
    public ServerEntryPoint(
        ISessionManager sessionManager,
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        ILogManager logManager,
        ILastfmApiClient lastfmApiClient)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;
        _libraryManager = libraryManager;
        _logger = logManager.GetLogger(GetType().Name);
        _lastfmApiClient = lastfmApiClient;
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

            // TODO: Implement "Now Playing" update
            // await _lastfmApiClient.UpdateNowPlayingAsync(...);
            await Task.CompletedTask; // Satisfy async warning
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

            // TODO: Implement scrobble submission
            // await _lastfmApiClient.ScrobbleAsync(...);
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

            // TODO: Implement love/unlove track
            // if (userData.IsFavorite)
            //     await _lastfmApiClient.LoveTrackAsync(...);
            // else
            //     await _lastfmApiClient.UnloveTrackAsync(...);
            await Task.CompletedTask; // Satisfy async warning
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
}
