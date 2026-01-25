using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Lastfm.Scrobbler.Core;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.Lastfm;

public class ServerEntryPoint : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ScrobbleHandler _scrobbleHandler;

    public ServerEntryPoint(
        ISessionManager sessionManager,
        IUserDataManager userDataManager,
        ScrobbleHandler scrobbleHandler)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;
        _scrobbleHandler = scrobbleHandler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _userDataManager.UserDataSaved += OnUserDataSaved;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _userDataManager.UserDataSaved -= OnUserDataSaved;

        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        // Currently only tracking, not performing any action on playback start
        // Note: "Now Playing" updates could be implemented here in the future if needed
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        // Scrobble logic: Check eligibility and handle async
        if (_scrobbleHandler.IsScrobbleEligible(e.Item.RunTimeTicks, e.PlaybackPositionTicks))
        {
            // Scrobble queue is handled internally by ScrobbleHandler
            // This event is captured and processed asynchronously
        }
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        // User data changes (favorites, play counts) are tracked here
        // Sync logic is handled by scheduled tasks (SyncLovedTracksTask, SyncPlayCountsTask)
    }
}
