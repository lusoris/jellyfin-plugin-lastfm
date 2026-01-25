// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Adapters;

using global::Lastfm.Scrobbler.Abstractions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Jellyfin implementation of playback event provider.
/// </summary>
public sealed partial class JellyfinPlaybackEventProvider : IPlaybackEventProvider, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<JellyfinPlaybackEventProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinPlaybackEventProvider"/> class.
    /// </summary>
    public JellyfinPlaybackEventProvider(
        ISessionManager sessionManager,
        IUserManager userManager,
        ILogger<JellyfinPlaybackEventProvider> logger)
    {
        _sessionManager = sessionManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<PlaybackStartedEventArgs>? PlaybackStarted;

    /// <inheritdoc />
    public event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;

    /// <summary>
    /// Starts listening to Jellyfin playback events.
    /// </summary>
    public void Start()
    {
        _sessionManager.PlaybackStart += OnJellyfinPlaybackStart;
        _sessionManager.PlaybackStopped += OnJellyfinPlaybackStopped;
        LogEventListeningStarted();
    }

    /// <summary>
    /// Stops listening to Jellyfin playback events.
    /// </summary>
    public void Stop()
    {
        _sessionManager.PlaybackStart -= OnJellyfinPlaybackStart;
        _sessionManager.PlaybackStopped -= OnJellyfinPlaybackStopped;
        LogEventListeningStopped();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    private void OnJellyfinPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        if (e.Users.Count == 0)
        {
            LogNoUsersForPlayback();
            return;
        }

        var user = e.Users[0];

        var eventArgs = new PlaybackStartedEventArgs
        {
            Item = AudioMapper.MapToDto(audio),
            UserId = user.Id,
            Username = user.Username,
            PositionTicks = e.PlaybackPositionTicks
        };

        PlaybackStarted?.Invoke(this, eventArgs);
    }

    private void OnJellyfinPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        if (e.Users.Count == 0)
        {
            LogNoUsersForPlayback();
            return;
        }

        var user = e.Users[0];

        var eventArgs = new PlaybackStoppedEventArgs
        {
            Item = AudioMapper.MapToDto(audio),
            UserId = user.Id,
            Username = user.Username,
            PlayedTicks = e.PlaybackPositionTicks ?? 0,
            PositionTicks = e.PlaybackPositionTicks
        };

        PlaybackStopped?.Invoke(this, eventArgs);
    }



    [LoggerMessage(Level = LogLevel.Information, Message = "Started listening to Jellyfin playback events")]
    private partial void LogEventListeningStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped listening to Jellyfin playback events")]
    private partial void LogEventListeningStopped();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Playback event received with no users")]
    private partial void LogNoUsersForPlayback();
}
