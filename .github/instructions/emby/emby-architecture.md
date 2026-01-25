---
applyTo: "**/Emby.*/**/*.cs"
description: Emby Plugin Architecture & Lifecycle
---

# Emby Plugin Architecture & Lifecycle

## Plugin Entry Points (Three Required Interfaces)

### 1. Plugin.cs - `BasePlugin<PluginConfiguration>` + `IHasWebPages`

```csharp
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override Guid Id => new Guid("5e7fe7f0-b048-429e-a431-b1a7e69c930d");
    public override string Name => "Last.fm";
    
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }
    
    public static Plugin Instance { get; private set; }
    
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Lastfm",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }
}
```

**Responsibilities**:
- Plugin metadata (ID, name, version)
- Configuration management
- Web UI pages
- Static instance for global access

**Lifecycle**: Instantiated once at Emby startup
**Access**: `Plugin.Instance` for global configuration access

**⚠️ Key Differences from Jellyfin**:
- Constructor parameters different (`IApplicationPaths` vs Jellyfin's paths)
- Must manually set `Instance` property
- No `IServerApplicationHost` injection here

---

### 2. ServerEntryPoint.cs - `IServerEntryPoint` + `IDisposable`

```csharp
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogManager _logManager;
    private readonly ILogger _logger;
    
    public ServerEntryPoint(
        ISessionManager sessionManager,
        IUserDataManager userDataManager,
        ILogManager logManager)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;
        _logManager = logManager;
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _userDataManager.UserDataSaved += OnUserDataSaved;
        
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _userDataManager.UserDataSaved -= OnUserDataSaved;
    }
    
    private async void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
    {
        // Handle playback start
    }
    
    private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
    {
        // Handle playback stop, scrobble logic
    }
    
    private async void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
    {
        // Handle favorite changes
    }
}
```

**Responsibilities**:
- Wire up event listeners
- Manage background operations
- Handle graceful shutdown

**Lifecycle**: 
- `RunAsync()` called when Emby starts
- `Dispose()` called on shutdown

**⚠️ Key Differences from Jellyfin**:
- Uses `IServerEntryPoint` (not `IHostedService`)
- `RunAsync()` instead of `StartAsync()`/`StopAsync()`
- No `CancellationToken` parameters
- Event unsubscription in `Dispose()` (not `StopAsync()`)
- `ILogManager` for logging (not `ILogger<T>`)

---

## Emby DI Container & Services

### Available Services (via Constructor Injection)

| Service | Purpose | Emby-Specific Notes |
|---------|---------|---------------------|
| `ISessionManager` | Playback events | Same as Jellyfin |
| `IUserDataManager` | User rating/favorite changes | Same as Jellyfin |
| `IUserManager` | User lookup | Same as Jellyfin |
| `ILibraryManager` | Audio item retrieval | Same as Jellyfin |
| `IHttpClient` | HTTP requests | ⚠️ NOT `IHttpClientFactory` |
| `ILogManager` | Logger creation | ⚠️ NOT `ILoggerFactory` |
| `IApplicationPaths` | Plugin data/config paths | Same as Jellyfin |
| `IXmlSerializer` | Configuration serialization | Same as Jellyfin |
| `IServerConfigurationManager` | Server config access | Emby-specific |

### DI Pattern

```csharp
// Emby auto-discovers IServerEntryPoint implementations
// No manual registration needed (unlike Jellyfin's IPluginServiceRegistrator)

// In ServerEntryPoint constructor:
public ServerEntryPoint(
    ISessionManager sessionManager,
    IHttpClient httpClient,         // Direct injection, no factory
    ILogManager logManager)         // Not ILoggerFactory
{
    _sessionManager = sessionManager;
    _httpClient = httpClient;        // Use directly
    _logger = logManager.GetLogger(GetType().Name);
}
```

**⚠️ Critical Difference: HTTP Client**
```csharp
// ❌ Jellyfin pattern (doesn't work in Emby):
_httpClient = httpClientFactory.CreateClient();

// ✅ Emby pattern:
_httpClient = httpClient;  // Inject IHttpClient directly
```

---

## Emby Event System

### PlaybackStart Event (ISessionManager)

```csharp
private async void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
{
    if (e.Item == null || e.Item.GetType().Name != "Audio") return;
    
    var users = e.Users;
    var item = e.Item as Audio;
    var position = e.PlaybackPositionTicks;
    
    // Send "Now Playing" to Last.fm
}
```

**Triggers**: When user presses play
**Use**: Send "now playing" to Last.fm

**⚠️ Differences from Jellyfin**:
- Type checking: `e.Item.GetType().Name != "Audio"` (not `is Audio`)
- .NET Framework 4.8 limitations (no pattern matching)

---

### PlaybackStopped Event (ISessionManager)

```csharp
private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    if (e.Item == null || e.Item.GetType().Name != "Audio") return;
    
    var position = e.PlaybackPositionTicks;  // Total played
    var item = e.Item as Audio;
    var duration = item.RunTimeTicks;
    
    // Check scrobble conditions
    if (!IsScrobbleEligible(duration, position)) return;
    
    // Submit scrobble to Last.fm
}
```

**Triggers**: When track ends or user stops playback
**Use**: Check scrobbling conditions, submit to Last.fm

**Scrobble Logic** (same as Jellyfin):
```csharp
private bool IsScrobbleEligible(long durationTicks, long playedTicks)
{
    var durationSeconds = TimeSpan.FromTicks(durationTicks).TotalSeconds;
    var playedSeconds = TimeSpan.FromTicks(playedTicks).TotalSeconds;
    
    // Must be at least 30 seconds long
    if (durationSeconds < 30) return false;
    
    // Must play for at least 4 minutes OR 50% of track
    return playedSeconds >= 240 || playedSeconds >= (durationSeconds * 0.5);
}
```

---

### UserDataSaved Event (IUserDataManager)

```csharp
private async void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
{
    if (e.Item == null || e.Item.GetType().Name != "Audio") return;
    
    var saveReason = e.SaveReason;
    var isFavorite = e.UserData.IsFavorite;
    var userId = e.UserId;
    
    // Handle favorite changes
}
```

**Triggers**: When user changes ratings/favorites
**SaveReasons**:
- `UserDataSaveReason.UpdateUserRating` - Favorite toggle
- `UserDataSaveReason.PlaybackFinished` - Alternative scrobble trigger

---

## Why `async void` is Correct

Event handlers **MUST** return `void`, not `Task`. Emby's events expect void delegates:

```csharp
private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    // This is CORRECT for Emby event handlers
    await _apiClient.Scrobble(item, lastfmUser).ConfigureAwait(false);
}
```

This is **not an anti-pattern** here—it's the only way to subscribe to void-returning events.

---

## Scheduled Tasks (IScheduledTask)

```csharp
public class SyncLovedTracksTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    
    public SyncLovedTracksTask(
        ILibraryManager libraryManager,
        ILogManager logManager)
    {
        _libraryManager = libraryManager;
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    public string Name => "Sync Loved Tracks from Last.fm";
    public string Key => "LastfmSyncLovedTracks";
    public string Description => "Import loved tracks from Last.fm";
    public string Category => "Last.fm";
    
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        };
    }
    
    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        progress.Report(0);
        
        // Fetch loved tracks from Last.fm
        var lovedTracks = await _apiClient.GetLovedTracksAsync(user);
        
        progress.Report(50);
        
        // Match and update in Jellyfin
        foreach (var track in lovedTracks)
        {
            var item = FindMatchingTrack(track);
            if (item != null)
            {
                var userData = _userDataManager.GetUserData(user, item);
                userData.IsFavorite = true;
                _userDataManager.SaveUserData(user.Id, item, userData, 
                    UserDataSaveReason.UpdateUserRating, cancellationToken);
            }
        }
        
        progress.Report(100);
    }
}
```

**Registration**: Emby auto-discovers `IScheduledTask` implementations

**⚠️ Differences from Jellyfin**:
- No explicit registration needed
- `TaskTriggerInfo` structure slightly different
- `ILogManager` for logging (not `ILogger<T>`)

---

## .NET Framework 4.8 Considerations

### 1. No Pattern Matching

```csharp
// ❌ Jellyfin (.NET 9.0):
if (e.Item is Audio audio)
{
    var artist = audio.Artists.FirstOrDefault();
}

// ✅ Emby (.NET Framework 4.8):
if (e.Item != null && e.Item.GetType().Name == "Audio")
{
    var audio = e.Item as Audio;
    var artist = audio.Artists != null ? audio.Artists.FirstOrDefault() : null;
}
```

### 2. No Nullable Reference Types

```csharp
// ❌ Jellyfin (.NET 9.0):
public string? Artist { get; set; }

// ✅ Emby (.NET Framework 4.8):
public string Artist { get; set; }  // May be null, no compiler warning
```

### 3. Async/Await Differences

```csharp
// Both work, but .NET Framework 4.8 has less sophisticated async machinery
// Use ConfigureAwait(false) more aggressively:
await _apiClient.ScrobbleAsync(item).ConfigureAwait(false);
```

### 4. LINQ Limitations

```csharp
// Some newer LINQ methods not available
// Use .ToList() more often to avoid deferred execution issues
```

---

## HTTP Client Usage (Critical Difference)

### Emby Pattern

```csharp
public class LastfmApiClient
{
    private readonly IHttpClient _httpClient;
    
    public LastfmApiClient(IHttpClient httpClient)
    {
        _httpClient = httpClient;  // Direct injection
    }
    
    public async Task<string> GetAsync(string url)
    {
        var options = new HttpRequestOptions
        {
            Url = url,
            CancellationToken = CancellationToken.None
        };
        
        using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
        {
            using (var stream = response.Content)
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
    
    public async Task<string> PostAsync(string url, Dictionary<string, string> data)
    {
        var options = new HttpRequestOptions
        {
            Url = url,
            RequestContent = new FormUrlEncodedContent(data).ReadAsStringAsync().Result,
            RequestContentType = "application/x-www-form-urlencoded",
            CancellationToken = CancellationToken.None
        };
        
        using (var response = await _httpClient.Post(options).ConfigureAwait(false))
        {
            using (var stream = response.Content)
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
```

**⚠️ Key Points**:
- `IHttpClient` (not `HttpClient` from `System.Net.Http`)
- `HttpRequestOptions` configuration object
- Manual stream reading
- No `IHttpClientFactory` pattern

---

## Logging Pattern

### Emby Pattern

```csharp
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly ILogger _logger;
    
    public ServerEntryPoint(ILogManager logManager)
    {
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    private void LogInfo(string message)
    {
        _logger.Info(message);
    }
    
    private void LogError(string message, Exception ex)
    {
        _logger.ErrorException(message, ex);
    }
    
    private void LogDebug(string message)
    {
        _logger.Debug(message);
    }
}
```

**⚠️ Differences from Jellyfin**:
- `ILogManager.GetLogger()` (not `ILoggerFactory.CreateLogger<T>()`)
- `_logger.Info()` (not `_logger.LogInformation()`)
- `_logger.ErrorException()` (not `_logger.LogError()`)
- No structured logging / LoggerMessage attributes

---

## Configuration Management

### Same Pattern as Jellyfin

```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    public LastfmUser[] LastfmUsers { get; set; }
    public bool SyncLovedTracks { get; set; }
    public bool SyncPlayCounts { get; set; }
    
    public PluginConfiguration()
    {
        LastfmUsers = Array.Empty<LastfmUser>();
        SyncLovedTracks = true;
        SyncPlayCounts = false;
    }
}

// Access:
var config = Plugin.Instance.Configuration;
Plugin.Instance.SaveConfiguration();
```

---

## Summary: Jellyfin vs Emby

| Aspect | Jellyfin | Emby |
|--------|----------|------|
| **Runtime** | .NET 9.0 | .NET Framework 4.8 |
| **Entry Point** | `IHostedService` | `IServerEntryPoint` |
| **Startup** | `StartAsync()` / `StopAsync()` | `RunAsync()` / `Dispose()` |
| **HTTP Client** | `IHttpClientFactory` | `IHttpClient` directly |
| **Logging** | `ILoggerFactory` + `ILogger<T>` | `ILogManager` + `ILogger` |
| **Log Methods** | `LogInformation()`, `LogError()` | `Info()`, `ErrorException()` |
| **Service Registration** | `IPluginServiceRegistrator` | Auto-discovery |
| **Pattern Matching** | ✅ Supported | ❌ Use type checks |
| **Nullable Types** | ✅ Supported | ❌ No compiler support |

---

**Related:**
- [emby-api.md](emby-api.md) - API reference & endpoints
- [emby-patterns.md](emby-patterns.md) - .NET Framework 4.8 patterns
- [../jellyfin-architecture.md](../jellyfin-architecture.md) - Compare with Jellyfin
