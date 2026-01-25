# Jellyfin Plugin Architecture & Lifecycle

## Plugin Entry Points (Three Required Interfaces)

### 1. Plugin.cs - `BasePlugin<PluginConfiguration>` + `IHasWebPages`

```csharp
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override Guid Id => new Guid("5e7fe7f0-b048-429e-a431-b1a7e69c930d");
    public override string Name => "Last.fm";
    
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[] { new PluginPageInfo { ... } };
    }
}
```

**Responsibilities**:
- Plugin metadata (ID, name, version)
- Configuration management
- Web UI pages

**Lifecycle**: Instantiated once at Jellyfin startup
**Access**: `Plugin.Instance` for global configuration access

---

### 2. PluginServiceRegistrator.cs - `IPluginServiceRegistrator`

```csharp
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, 
                                 IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHostedService<ServerEntryPoint>();
    }
}
```

**Responsibility**: Register services into Jellyfin's DI container

**Called**: During plugin initialization (one-time)

**Pattern**: Add all background services, HTTP clients, API clients here

---

### 3. ServerEntryPoint.cs - `IHostedService` + `IDisposable`

```csharp
public class ServerEntryPoint : IHostedService, IDisposable
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += PlaybackStart;
        _sessionManager.PlaybackStopped += PlaybackStopped;
        _userDataManager.UserDataSaved += UserDataSaved;
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= PlaybackStart;
        _sessionManager.PlaybackStopped -= PlaybackStopped;
        _userDataManager.UserDataSaved -= UserDataSaved;
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        // Cleanup resources
    }
}
```

**Responsibilities**:
- Wire up event listeners
- Manage background operations
- Handle graceful shutdown

**Lifecycle**: Started when Jellyfin starts, stopped on shutdown

**Event Binding**: Subscribe in `StartAsync`, unsubscribe in `StopAsync`

---

## Jellyfin DI Container & Services

### Available Services (via Constructor Injection)

| Service | Purpose |
|---------|---------|
| `ISessionManager` | Playback events (PlaybackStart, PlaybackStop) |
| `IUserDataManager` | User rating/favorite changes |
| `IUserManager` | User lookup and management |
| `ILibraryManager` | Audio item retrieval and matching |
| `IHttpClientFactory` | HTTP client pooling (MUST USE) |
| `ILoggerFactory` | Logger creation |
| `IApplicationPaths` | Plugin data/config paths |
| `IXmlSerializer` | Configuration serialization |

### DI Pattern

```csharp
// In PluginServiceRegistrator.RegisterServices:
serviceCollection.AddHostedService<ServerEntryPoint>();
serviceCollection.AddTransient<RestApi>();
serviceCollection.AddSingleton<LastfmApiClient>();

// In ServerEntryPoint constructor:
public ServerEntryPoint(
    ISessionManager sessionManager,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory,
    IUserDataManager userDataManager)
{
    _sessionManager = sessionManager;
    _httpClient = httpClientFactory.CreateClient();  // ALWAYS use factory
    _logger = loggerFactory.CreateLogger<ServerEntryPoint>();
}
```

---

## Jellyfin Event System

### PlaybackStart Event (ISessionManager)

```csharp
private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
{
    if (e.Item is not Audio) return;
    var users = e.Users;
    var item = e.Item as Audio;
    var position = e.PlaybackPositionTicks;
}
```

**Triggers**: When user presses play
**Use**: Send "now playing" to Last.fm

---

### PlaybackStopped Event (ISessionManager)

```csharp
private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    if (e.Item is not Audio) return;
    var position = e.PlaybackPositionTicks;  // Total played
    var item = e.Item as Audio;
    var duration = item.RunTimeTicks;
}
```

**Triggers**: When track ends or user stops playback
**Use**: Check scrobbling conditions, submit to Last.fm

---

### UserDataSaved Event (IUserDataManager)

```csharp
private async void UserDataSaved(object sender, UserDataSaveEventArgs e)
{
    if (e.Item is not Audio) return;
    var saveReason = e.SaveReason;
    var isFavorite = e.UserData.IsFavorite;
    var userId = e.UserId;
}
```

**Triggers**: When user changes ratings/favorites
**SaveReasons**:
- `UserDataSaveReason.UpdateUserRating` - Favorite toggle
- `UserDataSaveReason.PlaybackFinished` - Alternative scrobble trigger

---

## Why `async void` is Correct

Event handlers **MUST** return `void`, not `Task`. Jellyfin's events expect void delegates:

```csharp
private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    // This is CORRECT for Jellyfin event handlers
    await _apiClient.Scrobble(item, lastfmUser).ConfigureAwait(false);
}
```

This is **not an anti-pattern** here—it's the only way to subscribe to void-returning events. Exceptions are handled by `ConfigureAwait(false)`.

---

## Scheduled Tasks (IScheduledTask)

```csharp
public class ImportLastfmData : IScheduledTask
{
    public string Name => "Import Last.fm Loved Tracks";
    public string Key => "ImportLastfmData";
    public string Category => "Last.fm";
    
    public async Task ExecuteAsync(IProgress<double> progress, 
                                    CancellationToken cancellationToken)
    {
        Plugin.Syncing = true;  // Prevent real-time events during import
        try
        {
            foreach (var user in _userManager.GetUsers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(currentPercent);
                
                var lovedTracks = await _apiClient.GetLovedTracks(lastfmUser);
                foreach (var track in lovedTracks)
                {
                    var libraryItem = await MatchTrackToLibrary(track);
                    if (libraryItem != null)
                    {
                        _userDataManager.SaveUserData(userId, libraryItem, 
                            new UserItemData { IsFavorite = true });
                    }
                }
            }
        }
        finally
        {
            Plugin.Syncing = false;
        }
    }
    
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Enumerable.Empty<TaskTriggerInfo>();  // Manual only
    }
}
```

**Key Pattern**: Set `Plugin.Syncing = true` to suppress real-time events

---

## Metadata Providers (IRemoteImageProvider)

```csharp
public class LastfmArtistProvider : IRemoteImageProvider, IHasOrder
{
    public string Name => "Last.fm";
    public int Order => 10;  // Higher = runs first
    
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicArtist artist) 
            return Enumerable.Empty<RemoteImageInfo>();
        
        var musicBrainzId = artist.GetProviderId("MusicBrainzArtist");
        if (string.IsNullOrEmpty(musicBrainzId)) 
            return Enumerable.Empty<RemoteImageInfo>();
        
        var images = new List<RemoteImageInfo>();
        var lastfmData = await GetLastfmData(musicBrainzId, cancellationToken);
        
        if (lastfmData.ImageUrl != null)
        {
            images.Add(new RemoteImageInfo
            {
                Url = lastfmData.ImageUrl,
                ProviderName = Name,
                Type = ImageType.Primary
            });
        }
        
        return images;
    }
    
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken ct)
    {
        return _httpClientFactory.CreateClient().GetAsync(url, ct);
    }
    
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[] { ImageType.Primary, ImageType.Backdrop };
    }
}
```

**Integration**:
- Register in DI: `serviceCollection.AddSingleton<IRemoteImageProvider, LastfmArtistProvider>();`
- Jellyfin calls automatically during metadata refresh
- Higher `Order` = runs first

---

## REST Endpoints (ApiController)

```csharp
[ApiController]
[Route("Lastfm/Login")]
public class RestApi : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    public async Task<MobileSessionResponse> CreateMobileSession(
        [FromBody] LastFMUser user)
    {
        _logger.LogInformation("Auth request for {0}", user.Username);
        var session = await _apiClient.RequestSession(user.Username, user.Password);
        
        var config = Plugin.Instance.PluginConfiguration;
        var lastfmUser = new LastfmUser
        {
            Username = user.Username,
            SessionKey = session.SessionKey,
            MediaBrowserUserId = userId
        };
        config.LastfmUsers = config.LastfmUsers.Append(lastfmUser).ToArray();
        
        return session;
    }
}
```

**Pattern**:
- `[ApiController]` decorator
- `[Route("PluginName/Endpoint")]` routing
- Services injected, `Plugin.Instance` available

---

**Related**: 
- [copilot-instructions.md](copilot-instructions.md) - Overview & index
- [lastfm-api.instructions.md](lastfm-api.instructions.md) - API details
---

## Available Interfaces for Plugin Features

### Automatic Discovery Interfaces (Implement to Add Features)

These interfaces are automatically discovered by Jellyfin at startup. Just implement them and they work:

| Interface | Purpose |
|-----------|---------|
| `IAuthenticationProvider` | Add authentication methods (LDAP, OAuth, etc.) |
| `IBaseItemComparer` | Add sorting rules for media |
| `IIntroProvider` | Play media before movies/shows (trailers, bumpers) |
| `IItemResolver` | Define custom media types |
| `ILibraryPostScanTask` | Run tasks after library scan completes |
| `IMetadataSaver` | Write metadata in custom formats |
| `IResolverIgnoreRule` | Define paths to ignore during library scan |
| `IScheduledTask` | Add tasks to the scheduled tasks dashboard |
| `IRemoteImageProvider` | Fetch images from external sources |
| `IRemoteMetadataProvider<T>` | Fetch metadata from external sources |
| `IExternalId` | Add external ID providers (MusicBrainz, TheMovieDB) |

### Plugin-Specific Interfaces (Require Manual Registration)

| Interface | Purpose |
|-----------|---------|
| `IPluginServiceRegistrator` | Register services into DI container |
| `IPluginConfigurationPage` | Custom config page on dashboard |
| `IHasWebPages` | Plugin has web UI pages |
| `IHostedService` | Background service (starts/stops with Jellyfin) |
| `ControllerBase` | Custom REST API endpoints |

### Jellyfin Services (Available via DI)

| Service | Purpose |
|---------|---------|
| `IBlurayExaminer` | Examine Blu-ray folders |
| `IDtoService` | Create DTOs for API transport |
| `ILibraryManager` | Access media library directly |
| `ILocalizationManager` | Translations, ratings, units |
| `INetworkManager` | Server networking info |
| `IServerApplicationPaths` | Server paths (plugins, data, config) |
| `IServerConfigurationManager` | Server configuration read/write |
| `ITaskManager` | Execute/manage scheduled tasks |
| `IUserManager` | User retrieval and management |
| `IXmlSerializer` | XML serialization for config |
| `IZipClient` | Compression/decompression |

**Source**: [Jellyfin Plugin Template](https://github.com/jellyfin/jellyfin-plugin-template)

---

**Related:**
- [jellyfin-api.index.md](jellyfin-api.index.md) - API interfaces reference
- [jellyfin-configuration.md](jellyfin-configuration.md) - Configuration system
- [jellyfin-models.md](jellyfin-models.md) - Data models
- [emby/emby-architecture.md](emby/emby-architecture.md) - Emby equivalent architecture
- [workflow/development-workflow.md](workflow/development-workflow.md) - Development practices
