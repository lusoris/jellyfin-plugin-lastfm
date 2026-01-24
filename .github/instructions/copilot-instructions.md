# Jellyfin Last.fm Plugin - Comprehensive AI Coding Agent Instructions

## See Also
- [lastfm-api-instructions.md](lastfm-api-instructions.md) - Detailed Last.fm API endpoints, authentication, error codes, scrobbling rules
- [snyk_rules.instructions.md](snyk_rules.instructions.md) - Security scanning with Snyk

---

## 1. PROJECT OVERVIEW

### Basic Information
- **Name**: Jellyfin Last.fm Plugin
- **Purpose**: Scrobble music playback to Last.fm, sync loved tracks, provide metadata enrichment
- **Type**: C# .NET 9.0 Jellyfin Plugin
- **Target Jellyfin**: 10.11.6.0 (ABI)
- **Repository**: https://github.com/lusoris/jellyfin-plugin-lastfm
- **Latest Release**: v10.1.0
- **Status**: Actively maintained fork (forked from jesseward/jellyfin-plugin-lastfm)

### What It Does
1. **Scrobbling**: Captures music playback events and sends to Last.fm API
2. **Loved Tracks Sync**: Imports user's Last.fm loved songs into Jellyfin library favorites
3. **Metadata Enrichment**: Fetches artist images, album art, and biographical info from Last.fm
4. **Session Management**: Secure authentication using Last.fm API session keys

---

## 2. QUICK START

### Clone & Build
```bash
git clone https://github.com/lusoris/jellyfin-plugin-lastfm.git
cd jellyfin-plugin-lastfm
dotnet build Jellyfin.Plugin.Lastfm/Jellyfin.Plugin.Lastfm.csproj
```

### Project Structure
```
Jellyfin.Plugin.Lastfm/
├── Plugin.cs                          # Plugin entry point (BasePlugin<T>, IHasWebPages)
├── PluginServiceRegistrator.cs        # DI service registration (IPluginServiceRegistrator)
├── ServerEntryPoint.cs                # IHostedService main handler
├── Api/
│   ├── BaseLastfmApiClient.cs         # HTTP client base + serialization
│   ├── LastfmApiClient.cs             # Specific API endpoints
│   └── RestApi.cs                     # Jellyfin REST endpoints (/Lastfm/Login)
├── Models/
│   ├── LastfmUser.cs                  # User + SessionKey storage
│   ├── Requests/                      # BaseRequest subclasses (API requests)
│   └── Responses/                     # BaseResponse subclasses (API responses)
├── Providers/
│   ├── LastfmArtistProvider.cs        # IRemoteImageProvider implementation
│   ├── LastfmAlbumProvider.cs         # Album metadata provider
│   ├── LastfmImageProvider.cs         # Image utilities
│   └── Extensions.cs                  # LINQ/utility extensions
├── ScheduledTasks/
│   └── ImportLastfmData.cs            # IScheduledTask for loved tracks sync
├── Configuration/
│   ├── PluginConfiguration.cs         # XML config model (BasePluginConfiguration)
│   └── configPage.html                # Web UI for settings
├── Utils/
│   ├── Helpers.cs                     # MD5 signature, general helpers
│   ├── StringHelper.cs                # String utilities
│   └── UserHelpers.cs                 # User lookup and management
└── Resources/
    └── Strings.cs                     # Localization strings
```

---

## 3. JELLYFIN PLUGIN ARCHITECTURE

### Plugin Lifecycle (Three Required Interfaces)

#### 1. **Plugin.cs** - `BasePlugin<PluginConfiguration>` + `IHasWebPages`
```csharp
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public override Guid Id => new Guid("5e7fe7f0-b048-429e-a431-b1a7e69c930d");
    public override string Name => "Last.fm";
    
    public IEnumerable<PluginPageInfo> GetPages()  // Provide web UI
    {
        return new[] { new PluginPageInfo { ... } };
    }
}
```
- **Responsibilities**: Plugin metadata, ID, name, configuration, web pages
- **Lifecycle**: Instantiated once at Jellyfin startup
- **Static Access**: `Plugin.Instance` for accessing configuration globally

#### 2. **PluginServiceRegistrator.cs** - `IPluginServiceRegistrator`
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
- **Responsibility**: Register services into Jellyfin's DI container
- **Called Once**: During plugin initialization
- **Pattern**: Add all background services, HTTP clients, etc. here

#### 3. **ServerEntryPoint.cs** - `IHostedService` + `IDisposable`
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
        // Unsubscribe events
        _sessionManager.PlaybackStart -= PlaybackStart;
        _sessionManager.PlaybackStopped -= PlaybackStopped;
        _userDataManager.UserDataSaved -= UserDataSaved;
        return Task.CompletedTask;
    }
}
```
- **Responsibilities**: Wire up event listeners, manage background operations
- **Lifecycle**: Started when Jellyfin starts, stopped on shutdown
- **Event Binding**: Happens in `StartAsync`, cleanup in `StopAsync`

---

## 4. JELLYFIN DEPENDENCY INJECTION & SERVICES

### Available DI Services (Injected via Constructor)

| Service | Interface | Purpose |
|---------|-----------|---------|
| `ISessionManager` | Jellyfin | Playback events (PlaybackStart, PlaybackStop) |
| `IUserDataManager` | Jellyfin | User rating/favorite changes |
| `IUserManager` | Jellyfin | User lookup and management |
| `ILibraryManager` | Jellyfin | Audio item retrieval and matching |
| `IHttpClientFactory` | Microsoft | HTTP client pooling (MUST USE - not direct HttpClient) |
| `ILoggerFactory` | Microsoft | Logger creation |
| `IApplicationPaths` | Jellyfin | Plugin data/config paths |
| `IXmlSerializer` | Jellyfin | Configuration serialization |

### DI Pattern Usage
```csharp
// In PluginServiceRegistrator.RegisterServices:
serviceCollection.AddHostedService<ServerEntryPoint>();
serviceCollection.AddTransient<RestApi>();

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

## 5. JELLYFIN EVENT SYSTEM

### Playback Events (ISessionManager)

#### `PlaybackStart` Event
```csharp
private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
{
    if (e.Item is not Audio) return;  // Only process audio
    var users = e.Users;               // Current users
    var item = e.Item as Audio;        // Audio item
    var position = e.PlaybackPositionTicks;
}
```
**Triggers**: When user presses play
**Use**: Send "now playing" notification to Last.fm

#### `PlaybackStopped` Event
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
**Use**: Check scrobbling conditions, send to Last.fm if eligible

#### `UserDataSaved` Event (IUserDataManager)
```csharp
private async void UserDataSaved(object sender, UserDataSaveEventArgs e)
{
    if (e.Item is not Audio) return;
    var saveReason = e.SaveReason;  // UpdateUserRating, PlaybackFinished, etc
    var isFavorite = e.UserData.IsFavorite;
    var userId = e.UserId;
}
```
**Triggers**: When user changes ratings/favorites
**SaveReasons**:
- `UserDataSaveReason.UpdateUserRating` - Favorite toggle
- `UserDataSaveReason.PlaybackFinished` - Alternative scrobble mode trigger

### Why `async void` is Correct Here
Event handlers **MUST** return `void`, not `Task`. When async operations are needed:
```csharp
private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    // This is the CORRECT pattern for Jellyfin event handlers
    await _apiClient.Scrobble(item, lastfmUser).ConfigureAwait(false);
    // Exceptions are logged by ConfigureAwait(false), not propagated
}
```
This is **not an anti-pattern** here—it's the only way to subscribe to void-returning events.

---

## 6. JELLYFIN MODELS & TYPES

### Audio Item (MediaBrowser.Controller.Entities.Audio.Audio)
```csharp
var audio = item as Audio;  // Cast session/library items
audio.Name              // Track name (required for scrobbling)
audio.Artists           // Artist names (IList<string>)
audio.Album             // Album name
audio.RunTimeTicks      // Duration (100-nanosecond ticks)
audio.Path              // File path
audio.ParentId          // Album/folder ID
audio.ProviderIds       // MusicBrainz, MusicBrainzArtist, etc
```

### User Data (UserItemData)
```csharp
var userData = e.UserData;
userData.IsFavorite     // Is marked as favorite
userData.PlaybackTicks  // Seconds watched (for video)
userData.Rating         // User rating (0-10)
userData.Likes          // Like/Dislike state
```

### Jellyfin User (User)
```csharp
var user = e.Users.FirstOrDefault();
user.Username           // Jellyfin username
user.Id                 // Guid MediaBrowserUserId
```

---

## 7. CONFIGURATION & PERSISTENCE

### XML Configuration (BasePluginConfiguration)
```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    public LastfmUser[] LastfmUsers { get; set; }  // Per-user Last.fm config
}

public class LastfmUser
{
    public string Username { get; set; }           // Last.fm username
    public string SessionKey { get; set; }         // Permanent Last.fm session
    public Guid MediaBrowserUserId { get; set; }   // Links to Jellyfin user
    public LastFmUserOptions Options { get; set; }
}

public class LastFmUserOptions
{
    public bool Scrobble { get; set; }             // Enable scrobbling
    public bool SyncFavourites { get; set; }       // Enable loved tracks sync
    public bool AlternativeMode { get; set; }     // Scrobble on finish instead of progress
}
```

### Accessing Config
```csharp
var config = Plugin.Instance.PluginConfiguration;
var lastfmUser = config.LastfmUsers
    .FirstOrDefault(u => u.MediaBrowserUserId == userId);
```

---

## 8. LAST.FM API INTEGRATION

### API Client Architecture

The plugin uses a two-tier API client pattern:

#### BaseLastfmApiClient.cs - Generic HTTP Operations
- Handles POST requests to Last.fm endpoint
- Manages MD5 signature computation
- Serializes/deserializes JSON responses
- Implements error handling for API responses

**See also**: [lastfm-api-instructions.md](lastfm-api-instructions.md) for detailed API endpoint documentation

#### LastfmApiClient.cs - Specific Endpoints
- Implements individual Last.fm methods
- `RequestSession()` - Authentication
- `Scrobble()` - Track submission
- `NowPlaying()` - Current track info
- `GetLovedTracks()` - Sync user favorites
- `LoveTrack()` - Mark as loved

### Key Patterns
- All requests require API key + MD5 signature
- Session keys are permanent (exchange password once, never store password)
- Rate limiting: ~1000 requests/minute (space requests 1sec apart)
- Duplicate prevention: 15-second window per artist+track

**For detailed API endpoint info, authentication flow, error codes, and scrobbling rules**, see [lastfm-api-instructions.md](lastfm-api-instructions.md)

---

## 9. SCHEDULED TASKS (IScheduledTask)

### ImportLastfmData.cs Pattern
```csharp
public class ImportLastfmData : IScheduledTask
{
    public string Name => "Import Last.fm Loved Tracks";
    public string Key => "ImportLastfmData";
    public string Category => "Last.fm";
    
    public async Task ExecuteAsync(IProgress<double> progress, 
                                    CancellationToken cancellationToken)
    {
        Plugin.Syncing = true;  // Prevent real-time scrobbles during import
        try
        {
            foreach (var user in _userManager.GetUsers())
            {
                cancellationToken.ThrowIfCancellationRequested();  // Support cancellation
                progress.Report(currentPercent);  // Update UI progress
                
                var lovedTracks = await _apiClient.GetLovedTracks(lastfmUser);
                foreach (var track in lovedTracks)
                {
                    // Match to library by artist/album/title
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
        return Enumerable.Empty<TaskTriggerInfo>();  // Manual trigger only
    }
}
```

**Key Points**:
- Set `Plugin.Syncing = true` to suppress real-time events
- Use `cancellationToken` throughout for graceful shutdown
- Report progress via `IProgress<double>` (0-100)
- Return empty triggers for manual-only tasks

---

## 10. METADATA PROVIDERS (IRemoteImageProvider)

### Pattern Overview
```csharp
public class LastfmArtistProvider : IRemoteImageProvider, IHasOrder
{
    public string Name => "Last.fm";
    public int Order => 10;  // Priority (higher = earlier)
    
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicArtist artist) return Enumerable.Empty<RemoteImageInfo>();
        
        var musicBrainzId = artist.GetProviderId("MusicBrainzArtist");
        if (string.IsNullOrEmpty(musicBrainzId)) return Enumerable.Empty<RemoteImageInfo>();
        
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

**Integration Points**:
- Register in DI: `serviceCollection.AddSingleton<IRemoteImageProvider, LastfmArtistProvider>();`
- Jellyfin calls automatically during metadata refresh
- Higher `Order` value = runs first
- Use `GetProviderId()` to find MusicBrainz IDs

---

## 11. REST ENDPOINTS (ApiController)

### Last.fm Login Endpoint
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
        
        // Save to config
        var config = Plugin.Instance.PluginConfiguration;
        var lastfmUser = new LastfmUser
        {
            Username = user.Username,
            SessionKey = session.SessionKey,
            MediaBrowserUserId = userId  // From context
        };
        config.LastfmUsers = config.LastfmUsers.Append(lastfmUser).ToArray();
        // Config auto-saves
        
        return session;
    }
}
```

**Jellyfin Plugin API Pattern**:
- `[ApiController]` decorator
- `[Route("PluginName/Endpoint")]` routing
- Injected services available
- Plugin settings accessible via `Plugin.Instance`

---

## 12. C# & .NET PATTERNS USED

### Async/Await Best Practices
```csharp
// ✅ CORRECT: Always use ConfigureAwait(false) in library code
await _apiClient.Scrobble(item, user).ConfigureAwait(false);

// ✅ CORRECT: HttpClientFactory usage
var httpClient = _httpClientFactory.CreateClient();  // Pooled, reused
using var response = await httpClient.GetAsync(url);

// ✅ CORRECT: CancellationToken support
public async Task DoWork(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await Task.Delay(100, cancellationToken);
}

// ❌ WRONG: Don't use Task.Result or .Wait() - causes deadlocks
// ❌ WRONG: Direct HttpClient instantiation (doesn't pool connections)
```

### Null-Coalescing & Pattern Matching
```csharp
// ✅ Modern C# null-coalescing
var artistName = item.Artists?.FirstOrDefault() ?? "Unknown";

// ✅ Pattern matching
if (e.Item is not Audio) return;  // Type filter + negation
var audio = e.Item as Audio;      // Safe cast

// ✅ String interpolation
_logger.LogInformation("Scrobbling {0} by {1}", item.Name, artist);
```

### LINQ & Collections
```csharp
var users = _userManager.GetUsers()
    .Where(u => u.Policy.IsAdministrator)
    .ToList();

var firstSong = item.Artists.FirstOrDefault() ?? "Unknown";
var hasKey = config.LastfmUsers.Any(u => u.MediaBrowserUserId == userId);
```

### Logging Best Practices
```csharp
_logger.LogDebug("Debug info: {0}", detailVar);      // Verbose
_logger.LogInformation("Starting scrobble");           // Normal
_logger.LogWarning("Rate limit approaching");          // Issues
_logger.LogError(ex, "Scrobble failed: {0}", user);   // Exceptions
```

---

## 13. SECURITY PATTERNS

### Password Handling
```csharp
// ❌ NEVER store passwords
// ✅ Exchange for session key once, store key permanently
var response = await _apiClient.RequestSession(username, password);
// After this, only store: response.SessionKey
lastfmUser.SessionKey = response.SessionKey;
lastfmUser.Username = null;  // Don't store username either
```

### HTTP Security
```csharp
// ✅ Always use HttpClientFactory (automatic socket exhaustion prevention)
var client = _httpClientFactory.CreateClient();

// ✅ Always set timeouts
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await client.GetAsync(url, cts.Token);

// ✅ Validate SSL/TLS (HttpClientFactory defaults are secure)
```

### Configuration Security
```csharp
// ✅ Never log sensitive data
_logger.LogInformation("User {0} configured", user.Username);  // OK
_logger.LogDebug("Session key: {0}", sessionKey);              // ❌ DON'T

// ✅ Store sensitive data only in config (XML on disk, restricted perms)
```

---

## 13. BUILD & DEPLOYMENT

### Build Process
```bash
# Build single project
dotnet build Jellyfin.Plugin.Lastfm

# Build release
dotnet build -c Release

# Build zip for distribution
# (Jellyfin meta-plugins workflow does this automatically)
```

### Version Strategy
- **Assembly Version**: 10.1.0.0 (major.minor.patch.build)
- **Target ABI**: 10.11.6.0 (Jellyfin version)
- **Tag Format**: v10.1.0 (for GitHub releases)
- **Manifest Version**: 10.1.0.0

### Release Workflow (GitHub Actions)
1. Push tag: `git tag -a v10.1.0 && git push origin v10.1.0`
2. Workflow triggers: `create-github-release.yml`
3. Builds release: `dotnet build -c Release`
4. Creates ZIP: `lastfm_v10.1.0.zip`
5. Updates manifest: `manifest.json` with new version
6. Creates GitHub Release with ZIP attached
7. (Optional) Uploads to Azure blob storage

---

## 14. TESTING STRATEGY

### Current State
- No formal unit test project
- Test audio files in `tests/` directory (for manual testing)
- Integration tested via Jellyfin instance

### What Should Be Tested
```csharp
// API Client tests (mocked HTTP)
- RequestSession (success/failure)
- Scrobble (MD5 signature, request format)
- GetLovedTracks (pagination)

// Scrobbling Logic tests
- Duration/playtime threshold validation
- Duplicate scrobble prevention (15-second window)
- User preference filtering (Scrobble, SyncFavourites)

// Configuration tests
- XML persistence and loading
- User lookup by Jellyfin ID
```

---

## 15. SNYK SECURITY SCANNING

### Pre-Commit Security Checklist
Before PR:
1. **Run**: `snyk code scan` (must be configured)
2. **Review**: Focus areas:
   - HTTP client usage (no direct instantiation)
   - JSON deserialization (potential XXE, JSON bombs)
   - String formatting (no concatenation with user input)
   - Password/key handling (never log, never transmit cleartext)
3. **Fix**: Address all High/Critical findings
4. **Rescan**: Verify fixes with `snyk code scan` again

---

## 16. KEY FILES REFERENCE

| File | Purpose | Key Classes |
|------|---------|-------------|
| [Plugin.cs](/jellyfin-plugin-lastfm/Plugin.cs) | Plugin metadata & config UI | `Plugin` |
| [PluginServiceRegistrator.cs](/jellyfin-plugin-lastfm/PluginServiceRegistrator.cs) | DI registration | `PluginServiceRegistrator` |
| [ServerEntryPoint.cs](/jellyfin-plugin-lastfm/ServerEntryPoint.cs) | Event listening & scrobbling | `ServerEntryPoint` |
| [BaseLastfmApiClient.cs](/jellyfin-plugin-lastfm/Api/BaseLastfmApiClient.cs) | HTTP + MD5 signature | `BaseLastfmApiClient` |
| [LastfmApiClient.cs](/jellyfin-plugin-lastfm/Api/LastfmApiClient.cs) | API endpoints | `LastfmApiClient` |
| [RestApi.cs](/jellyfin-plugin-lastfm/Api/RestApi.cs) | Login endpoint | `RestApi` |
| [PluginConfiguration.cs](/jellyfin-plugin-lastfm/Configuration/PluginConfiguration.cs) | Config model | `PluginConfiguration`, `LastfmUser` |
| [ImportLastfmData.cs](/jellyfin-plugin-lastfm/ScheduledTasks/ImportLastfmData.cs) | Sync scheduled task | `ImportLastfmData` |
| [LastfmArtistProvider.cs](/jellyfin-plugin-lastfm/Providers/LastfmArtistProvider.cs) | Artist metadata | `LastfmArtistProvider` |
| [Helpers.cs](/jellyfin-plugin-lastfm/Utils/Helpers.cs) | MD5 signature generation | Signature logic |
| [build.yaml](/build.yaml) | Plugin metadata for distribution | Version, ABI target |

---

## 17. COMMON TASKS & PATTERNS

### Add a New Scrobbling Event
```csharp
// 1. Define event handler in ServerEntryPoint
private async void MyNewEvent(object sender, MyEventArgs e)
{
    if (e.Item is not Audio) return;
    var user = GetUser(e.UserId);
    if (!user?.Options.Scrobble ?? true) return;
    
    await _apiClient.Scrobble(e.Item as Audio, user).ConfigureAwait(false);
}

// 2. Subscribe in StartAsync
_sessionManager.MyEvent += MyNewEvent;

// 3. Unsubscribe in StopAsync
_sessionManager.MyEvent -= MyNewEvent;
```

### Add Configuration Option
```csharp
// 1. Extend LastFmUserOptions
public class LastFmUserOptions
{
    public bool MyNewOption { get; set; }
}

// 2. Check in logic
if (!lastfmUser.Options.MyNewOption) return;

// 3. Add to web UI (configPage.html)
<label>
    <input type="checkbox" ng-model="config.LastfmUsers[0].Options.MyNewOption">
    My New Option
</label>
```

### Add New Last.fm API Call
```csharp
// 1. Create Request class (Models/Requests/MyNewRequest.cs)
public class MyNewRequest : BaseRequest
{
    public string Method => "my.newmethod";
    public string SessionKey { get; set; }
    public string Param1 { get; set; }
}

// 2. Create Response class
public class MyNewResponse : BaseResponse
{
    public string Result { get; set; }
}

// 3. Add method to LastfmApiClient
public async Task<MyNewResponse> MyNewMethod(string param)
{
    var request = new MyNewRequest { Param1 = param };
    return await Post<MyNewRequest, MyNewResponse>(request);
}
```

---

## 18. KNOWN LIMITATIONS & FUTURE WORK

### Current Limitations
1. **No Scrobbling Threshold Customization**: Hardcoded 30s, 4min, 50% (could be config)
2. **No Test Suite**: Only manual integration testing
3. **No Duplicate Hash**: Uses only 15-second window (could use track hash)
4. **Limited Error Recovery**: Failed API calls logged but not retried
5. **No Caching**: Fetches metadata fresh every time (could cache)

### Future Enhancements
- Unit test suite with mocked HTTP
- Configurable scrobbling thresholds
- Exponential backoff for API retries
- Track matching cache (MusicBrainz ID)
- Support for additional metadata sources
- Batch scrobbling for offline mode

---

## 19. DEVELOPMENT WORKFLOW

### Making Changes
1. **Create Feature Branch**: `git checkout -b feature/my-feature`
2. **Make Changes**: Edit files, follow patterns above
3. **Test Locally**: Build + install in Jellyfin dev instance
4. **Run Security Scan**: `snyk code scan`
5. **Commit**: `git commit -m "feat: Clear description"`
6. **Push**: `git push origin feature/my-feature`
7. **Create PR**: To `main` branch (not master)
8. **Tag Release**: `git tag -a v10.1.1` for new releases

### Commit Message Format
```
feat: Add configurable scrobbling thresholds
fix: Prevent duplicate scrobbles in 15s window
docs: Update scrobbling rules documentation
chore: Upgrade Jellyfin.Controller to 10.11.6
```

---

## 20. HELPFUL RESOURCES

### Official Documentation
- [Jellyfin Plugin Development](https://jellyfin.org/docs/general/server/plugins/)
- [Last.fm API Docs](https://www.last.fm/api)
- [.NET 9.0 Docs](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)

### This Project
- **Latest Release**: https://github.com/lusoris/jellyfin-plugin-lastfm/releases/tag/v10.1.0
- **Issue Tracker**: https://github.com/lusoris/jellyfin-plugin-lastfm/issues
- **GitHub Discussions**: https://github.com/lusoris/jellyfin-plugin-lastfm/discussions

---

**Last Updated**: January 24, 2026 | **Target Jellyfin**: 10.11.6 | **Framework**: .NET 9.0

---

## Related Files
- [lastfm-api-instructions.md](lastfm-api-instructions.md) - Last.fm API details
- [snyk_rules.instructions.md](snyk_rules.instructions.md) - Security scanning
