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
using Queue;
using Services;

/// <summary>
/// Handles playback events for scrobbling and now playing updates.
/// </summary>
public sealed partial class PlaybackEventHandler : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly ILastfmApiClient _apiClient;
    private readonly IScrobbleService _scrobbleService;
    private readonly IScrobbleQueue _scrobbleQueue;
    private readonly ILogger<PlaybackEventHandler> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackEventHandler"/> class.
    /// </summary>
    public PlaybackEventHandler(
        ISessionManager sessionManager,
        ILastfmApiClient apiClient,
        IScrobbleService scrobbleService,
        IScrobbleQueue scrobbleQueue,
        ILogger<PlaybackEventHandler> logger)
    {
        _sessionManager = sessionManager;
        _apiClient = apiClient;
        _scrobbleService = scrobbleService;
        _scrobbleQueue = scrobbleQueue;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;

        LogHandlerStarted();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;

        LogHandlerStopped();
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
            LogPlaybackStartError(ex);
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
            LogPlaybackStoppedError(ex);
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

        LogSendingNowPlaying(
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
            LogTrackNotEligible(
                audio.Artists.FirstOrDefault(),
                audio.Name,
                trackLengthTicks > 0 ? (double)playedTicks / trackLengthTicks * 100 : 0);
            return;
        }

        // Check for duplicate
        if (_scrobbleService.IsDuplicateScrobble(userId, audio.Id))
        {
            LogSkippingDuplicate(
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

        LogScrobbling(
            userConfig.Username,
            scrobbleInfo.Artist,
            scrobbleInfo.Track);

        try
        {
            var response = await _apiClient.ScrobbleAsync(scrobbleInfo, userConfig.SessionKey).ConfigureAwait(false);

            if (response?.Scrobbles?.Attributes?.Accepted > 0)
            {
                _scrobbleService.RecordScrobble(userId, audio.Id);
            }
            else if (response?.IsError == true && IsRetryableError(response.ErrorCode))
            {
                // Queue for retry on network/rate limit errors
                LogScrobbleFailedQueueing(
                    scrobbleInfo.Artist,
                    scrobbleInfo.Track,
                    response.Error?.Message ?? "Unknown");
                await _scrobbleQueue.EnqueueAsync(userId, scrobbleInfo).ConfigureAwait(false);
            }
        }
        catch (HttpRequestException ex)
        {
            // Network error - queue for retry
            LogNetworkErrorQueueing(
                ex,
                scrobbleInfo.Artist,
                scrobbleInfo.Track);
            await _scrobbleQueue.EnqueueAsync(userId, scrobbleInfo).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks if an error code is retryable (network/rate limit issues).
    /// </summary>
    private static bool IsRetryableError(int? errorCode)
    {
        // Error codes that should be retried:
        // 11 - Service temporarily unavailable
        // 16 - Temporary error
        // 29 - Rate limit exceeded
        return errorCode is 11 or 16 or 29;
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
            // Prefer MusicBrainzRecording (recording MBID) over MusicBrainzTrack (release-specific track MBID)
            // Last.fm uses recording MBIDs for track matching
            MusicBrainzId = audio.ProviderIds.TryGetValue("MusicBrainzRecording", out var mbid)
                ? mbid
                : audio.ProviderIds.TryGetValue("MusicBrainzTrack", out var trackMbid)
                    ? trackMbid
                    : null,
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
    private void Dispose(bool disposing)
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Last.fm playback event handler started")]
    private partial void LogHandlerStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Last.fm playback event handler stopped")]
    private partial void LogHandlerStopped();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling playback start event")]
    private partial void LogPlaybackStartError(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling playback stopped event")]
    private partial void LogPlaybackStoppedError(Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending now playing for {User}: {Artist} - {Track}")]
    private partial void LogSendingNowPlaying(string user, string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Track not eligible for scrobble: {Artist} - {Track} (played {Percent:F1}%)")]
    private partial void LogTrackNotEligible(string? artist, string? track, double percent);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping duplicate scrobble: {Artist} - {Track}")]
    private partial void LogSkippingDuplicate(string? artist, string? track);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scrobbling for {User}: {Artist} - {Track}")]
    private partial void LogScrobbling(string user, string artist, string track);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scrobble failed, queueing for retry: {Artist} - {Track} (Error: {Error})")]
    private partial void LogScrobbleFailedQueueing(string artist, string track, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Network error scrobbling, queueing for retry: {Artist} - {Track}")]
    private partial void LogNetworkErrorQueueing(Exception ex, string artist, string track);
}
