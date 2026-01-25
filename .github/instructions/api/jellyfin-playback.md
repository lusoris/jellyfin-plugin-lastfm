# Jellyfin Playback Events

Event data structures for playback handling.

## PlaybackProgressEventArgs

Base class for all playback events.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public class PlaybackProgressEventArgs : EventArgs
{
    public List<User> Users { get; set; }
    public long? PlaybackPositionTicks { get; set; }
    public BaseItem Item { get; set; }
    public bool IsPaused { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string ClientName { get; set; }
    public SessionInfo Session { get; set; }
}
```

## PlaybackStopEventArgs

Fired when playback ends.

```csharp
public class PlaybackStopEventArgs : PlaybackProgressEventArgs
{
    public bool PlayedToCompletion { get; set; }
}
```

## Scrobble Eligibility

```csharp
// Scrobble if:
// - Track length >= 30 seconds
// - Played >= 50% OR >= 4 minutes (whichever first)

bool ShouldScrobble(PlaybackStopEventArgs e)
{
    if (e.Item is not Audio audio) return false;
    
    var length = audio.RunTimeTicks ?? 0;
    var played = e.PlaybackPositionTicks ?? 0;
    
    // Minimum track length: 30 seconds
    if (length < TimeSpan.FromSeconds(30).Ticks) return false;
    
    // 50% threshold
    var percent = (double)played / length * 100;
    if (percent >= 50) return true;
    
    // 4 minute threshold
    return played >= TimeSpan.FromMinutes(4).Ticks;
}
```

## Event Subscription Pattern

```csharp
public class PlaybackHandler : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;

    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item is not Audio audio) return;
        // Update Now Playing...
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Item is not Audio audio) return;
        // Scrobble if eligible...
    }

    public void Dispose()
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
    }
}
```

---

**Related:** [jellyfin-interfaces.md](jellyfin-interfaces.md) | [jellyfin-audio.md](jellyfin-audio.md)
