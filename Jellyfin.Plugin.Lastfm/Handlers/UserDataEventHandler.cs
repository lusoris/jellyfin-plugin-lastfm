// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Handlers;

using Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;

/// <summary>
/// Handles user data changes for syncing favorites to Last.fm loved tracks.
/// </summary>
public sealed partial class UserDataEventHandler : IHostedService, IDisposable
{
    private readonly IUserDataManager _userDataManager;
    private readonly ILastfmApiClient _apiClient;
    private readonly ILogger<UserDataEventHandler> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataEventHandler"/> class.
    /// </summary>
    public UserDataEventHandler(
        IUserDataManager userDataManager,
        ILastfmApiClient apiClient,
        ILogger<UserDataEventHandler> logger)
    {
        _userDataManager = userDataManager;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved += OnUserDataSaved;

        LogHandlerStarted();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;

        LogHandlerStopped();
        return Task.CompletedTask;
    }

    private async void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        try
        {
            // Only handle audio items
            if (e.Item is not Audio audio)
            {
                return;
            }

            // Only handle favorite changes
            // IsFavorite can be changed via UpdateUserRating, UpdateUserData, or TogglePlayed
            if (e.SaveReason != UserDataSaveReason.UpdateUserRating &&
                e.SaveReason != UserDataSaveReason.UpdateUserData &&
                e.SaveReason != UserDataSaveReason.TogglePlayed)
            {
                return;
            }

            var config = Plugin.Instance?.Configuration;
            if (config == null || !config.IsConfigured)
            {
                return;
            }

            var userConfig = config.GetUserConfig(e.UserId);
            if (userConfig == null || !userConfig.HasValidSession)
            {
                return;
            }

            var isFavorite = e.UserData.IsFavorite;

            // Check if sync is enabled for this direction
            if (isFavorite && !userConfig.Options.SyncFavoritesToLoved)
            {
                return;
            }

            if (!isFavorite && !userConfig.Options.SyncUnfavoritesToUnloved)
            {
                return;
            }

            // Get artist and track name
            var artist = audio.Artists.FirstOrDefault();
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(audio.Name))
            {
                LogMissingArtistOrTrack();
                return;
            }

            if (isFavorite)
            {
                LogSyncingFavorite(
                    artist,
                    audio.Name,
                    userConfig.Username);

                await _apiClient.LoveTrackAsync(artist, audio.Name, userConfig.SessionKey).ConfigureAwait(false);
            }
            else
            {
                LogSyncingUnfavorite(
                    artist,
                    audio.Name,
                    userConfig.Username);

                await _apiClient.UnloveTrackAsync(artist, audio.Name, userConfig.SessionKey).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogUserDataSavedError(ex);
        }
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
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _userDataManager.UserDataSaved -= OnUserDataSaved;
            }

            _disposed = true;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Last.fm user data event handler started")]
    private partial void LogHandlerStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Last.fm user data event handler stopped")]
    private partial void LogHandlerStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cannot sync favorite: missing artist or track name")]
    private partial void LogMissingArtistOrTrack();

    [LoggerMessage(Level = LogLevel.Information, Message = "Syncing favorite to Last.fm loved: {Artist} - {Track} for {User}")]
    private partial void LogSyncingFavorite(string artist, string track, string user);

    [LoggerMessage(Level = LogLevel.Information, Message = "Syncing unfavorite to Last.fm unloved: {Artist} - {Track} for {User}")]
    private partial void LogSyncingUnfavorite(string artist, string track, string user);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling user data saved event")]
    private partial void LogUserDataSavedError(Exception ex);
}
