// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Handlers;

using Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using Services;

/// <summary>
/// Handles playback events for scrobbling and now playing updates.
/// </summary>
public class PlaybackEventHandler : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly ILastfmApiClient _apiClient;
    private readonly IScrobbleService _scrobbleService;
    private readonly ILogger<PlaybackEventHandler> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackEventHandler"/> class.
    /// </summary>
    public PlaybackEventHandler(
        ISessionManager sessionManager,
        ILastfmApiClient apiClient,
        IScrobbleService scrobbleService,
        ILogger<PlaybackEventHandler> logger)
    {
        _sessionManager = sessionManager;
        _apiClient = apiClient;
        _scrobbleService = scrobbleService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;

        _logger.LogInformation("Last.fm playback event handler started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;

        _logger.LogInformation("Last.fm playback event handler stopped");
        return Task.CompletedTask;
    }

    private async void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        try
        {
            // Only handle audio items
            if (e.Item is not Audio audio)
            {
                return;
            }

            // Process each user in the session
            foreach (var user in e.Users)
            {
                await SendNowPlayingAsync(user.Id, audio).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback start event");
        }
    }

    private async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        try
        {
            // Only handle audio items
            if (e.Item is not Audio audio)
            {
                return;
            }

            // Process each user in the session
            foreach (var user in e.Users)
            {
                await ScrobbleAsync(user.Id, audio, e.PlayedToCompletion, e.PlaybackPositionTicks ?? 0)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling playback stopped event");
        }
    }

    private async Task SendNowPlayingAsync(Guid userId, Audio audio)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
        {
            return;
        }

        var userConfig = config.GetUserConfig(userId);
        if (userConfig == null || !userConfig.HasValidSession)
        {
            return;
        }

        if (!userConfig.Options.NowPlayingEnabled)
        {
            return;
        }

        var scrobbleInfo = CreateScrobbleInfo(audio);
        if (scrobbleInfo == null)
        {
            return;
        }

        _logger.LogDebug(
            "Sending now playing for {User}: {Artist} - {Track}",
            userConfig.Username,
            scrobbleInfo.Artist,
            scrobbleInfo.Track);

        await _apiClient.UpdateNowPlayingAsync(scrobbleInfo, userConfig.SessionKey).ConfigureAwait(false);
    }

    private async Task ScrobbleAsync(Guid userId, Audio audio, bool playedToCompletion, long playedTicks)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
        {
            return;
        }

        var userConfig = config.GetUserConfig(userId);
        if (userConfig == null || !userConfig.HasValidSession)
        {
            return;
        }

        if (!userConfig.Options.ScrobbleEnabled)
        {
            return;
        }

        // Check if scrobble is eligible
        var trackLengthTicks = audio.RunTimeTicks ?? 0;
        if (!_scrobbleService.IsScrobbleEligible(trackLengthTicks, playedTicks) && !playedToCompletion)
        {
            _logger.LogDebug(
                "Track not eligible for scrobble: {Artist} - {Track} (played {Percent:F1}%)",
                audio.Artists.FirstOrDefault(),
                audio.Name,
                trackLengthTicks > 0 ? (double)playedTicks / trackLengthTicks * 100 : 0);
            return;
        }

        // Check for duplicate
        if (_scrobbleService.IsDuplicateScrobble(userId, audio.Id))
        {
            _logger.LogDebug(
                "Skipping duplicate scrobble: {Artist} - {Track}",
                audio.Artists.FirstOrDefault(),
                audio.Name);
            return;
        }

        var scrobbleInfo = CreateScrobbleInfo(audio);
        if (scrobbleInfo == null)
        {
            return;
        }

        // Set timestamp to when playback started (now minus played time)
        scrobbleInfo.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (playedTicks / TimeSpan.TicksPerSecond);

        _logger.LogInformation(
            "Scrobbling for {User}: {Artist} - {Track}",
            userConfig.Username,
            scrobbleInfo.Artist,
            scrobbleInfo.Track);

        var response = await _apiClient.ScrobbleAsync(scrobbleInfo, userConfig.SessionKey).ConfigureAwait(false);

        if (response?.Scrobbles?.Attributes?.Accepted > 0)
        {
            _scrobbleService.RecordScrobble(userId, audio.Id);
        }
    }

    private static ScrobbleInfo? CreateScrobbleInfo(Audio audio)
    {
        var artist = audio.Artists.FirstOrDefault();
        if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(audio.Name))
        {
            return null;
        }

        return new ScrobbleInfo
        {
            Artist = artist,
            Track = audio.Name,
            Album = audio.Album,
            AlbumArtist = audio.AlbumArtists.FirstOrDefault(),
            MusicBrainzId = audio.ProviderIds.TryGetValue("MusicBrainzTrack", out var mbid) ? mbid : null,
            Duration = audio.RunTimeTicks.HasValue
                ? (int)(audio.RunTimeTicks.Value / TimeSpan.TicksPerSecond)
                : null,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
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
                _sessionManager.PlaybackStart -= OnPlaybackStart;
                _sessionManager.PlaybackStopped -= OnPlaybackStopped;
            }

            _disposed = true;
        }
    }
}
