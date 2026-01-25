// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Handlers;

using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        // TODO: Implement now playing update
        // Note: async void is correct for event handlers
    }

    private async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        // TODO: Implement scrobbling
        // Note: async void is correct for event handlers
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
                // Unsubscribe from events
                _sessionManager.PlaybackStart -= OnPlaybackStart;
                _sessionManager.PlaybackStopped -= OnPlaybackStopped;
            }

            _disposed = true;
        }
    }
}
