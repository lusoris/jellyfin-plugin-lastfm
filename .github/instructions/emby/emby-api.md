---
applyTo: "**/Emby.*/**/*.cs"
description: Emby API Reference & Service Interfaces
---

# Emby API Reference

Complete reference for Emby Server APIs used in Last.fm plugin development.

## Core Services

### ISessionManager

**Purpose**: Monitor playback events and active sessions

#### Key Events

```csharp
public interface ISessionManager
{
    event EventHandler<PlaybackProgressEventArgs> PlaybackStart;
    event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;
    event EventHandler<PlaybackStopEventArgs> PlaybackStopped;
}
```

#### Event Args

```csharp
public class PlaybackProgressEventArgs : EventArgs
{
    public BaseItem Item { get; set; }          // The media item being played
    public User[] Users { get; set; }           // Users in the session
    public long? PlaybackPositionTicks { get; set; }  // Current position
    public SessionInfo Session { get; set; }    // Session info
}

public class PlaybackStopEventArgs : EventArgs
{
    public BaseItem Item { get; set; }
    public User[] Users { get; set; }
    public long? PlaybackPositionTicks { get; set; }  // Total played
    public SessionInfo Session { get; set; }
}
```

#### Usage Example

```csharp
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    
    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        return Task.CompletedTask;
    }
    
    private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
    {
        if (e.Item == null || e.Item.GetType().Name != "Audio") return;
        
        var audio = e.Item as Audio;
        var playedTicks = e.PlaybackPositionTicks ?? 0;
        var durationTicks = audio.RunTimeTicks ?? 0;
        
        // Check scrobble eligibility
        if (IsScrobbleEligible(durationTicks, playedTicks))
        {
            await ScrobbleAsync(audio, e.Users.FirstOrDefault()).ConfigureAwait(false);
        }
    }
}
```

---

### IUserDataManager

**Purpose**: Manage user-specific data (favorites, play counts, ratings)

#### Key Events

```csharp
public interface IUserDataManager
{
    event EventHandler<UserDataSaveEventArgs> UserDataSaved;
    
    UserItemData GetUserData(User user, BaseItem item);
    UserItemData GetUserData(Guid userId, Guid itemId);
    void SaveUserData(Guid userId, BaseItem item, UserItemData userData, 
                     UserDataSaveReason reason, CancellationToken cancellationToken);
}
```

#### UserItemData Properties

```csharp
public class UserItemData
{
    public bool IsFavorite { get; set; }           // Loved/favorite status
    public int PlayCount { get; set; }             // Number of plays
    public DateTime? LastPlayedDate { get; set; }  // Last play timestamp
    public long? PlaybackPositionTicks { get; set; }  // Resume point
    public bool Played { get; set; }               // Marked as played
}
```

#### UserDataSaveEventArgs

```csharp
public class UserDataSaveEventArgs : EventArgs
{
    public Guid UserId { get; set; }
    public BaseItem Item { get; set; }
    public UserItemData UserData { get; set; }
    public UserDataSaveReason SaveReason { get; set; }
}

public enum UserDataSaveReason
{
    PlaybackStart,
    PlaybackProgress,
    PlaybackFinished,
    TogglePlayed,
    UpdateUserRating    // Favorite changes
}
```

#### Usage Example

```csharp
private async void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
{
    // Only care about favorites on Audio items
    if (e.Item == null || e.Item.GetType().Name != "Audio") return;
    if (e.SaveReason != UserDataSaveReason.UpdateUserRating) return;
    
    var audio = e.Item as Audio;
    var user = _userManager.GetUserById(e.UserId);
    
    if (e.UserData.IsFavorite)
    {
        await _apiClient.LoveTrackAsync(audio, user).ConfigureAwait(false);
    }
    else
    {
        await _apiClient.UnloveTrackAsync(audio, user).ConfigureAwait(false);
    }
}
```

---

### ILibraryManager

**Purpose**: Query and retrieve media items from library

#### Key Methods

```csharp
public interface ILibraryManager
{
    BaseItem GetItemById(Guid id);
    BaseItem GetItemById(Guid id, Guid? userId);
    
    List<BaseItem> GetItemList(InternalItemsQuery query);
    QueryResult<BaseItem> GetItemsResult(InternalItemsQuery query);
    
    Task<ItemResolveArgs> ResolvePath(FileSystemMetadata fileInfo, Folder parent = null);
}
```

#### InternalItemsQuery (Filtering)

```csharp
var query = new InternalItemsQuery(user)
{
    IncludeItemTypes = new[] { "Audio" },
    Recursive = true,
    HasAnyProviderId = new Dictionary<string, string>
    {
        ["MusicBrainzRecording"] = "mbid-value"
    },
    ArtistIds = new[] { artistId },
    AlbumIds = new[] { albumId },
    Limit = 100,
    SortBy = new[] { ItemSortBy.Name },
    SortOrder = SortOrder.Ascending
};

var items = _libraryManager.GetItemList(query);
```

#### Usage Example: Find Track

```csharp
public Audio FindMatchingTrack(string artist, string track, Guid userId)
{
    var user = _userManager.GetUserById(userId);
    
    var query = new InternalItemsQuery(user)
    {
        IncludeItemTypes = new[] { "Audio" },
        Recursive = true,
        SearchTerm = track,  // Pre-filter by track name
        Limit = 100
    };
    
    var items = _libraryManager.GetItemList(query);
    
    return items
        .OfType<Audio>()
        .FirstOrDefault(a =>
            string.Equals(a.Name, track, StringComparison.OrdinalIgnoreCase) &&
            (a.Artists.Any(art => string.Equals(art, artist, StringComparison.OrdinalIgnoreCase)) ||
             a.AlbumArtists.Any(aa => string.Equals(aa, artist, StringComparison.OrdinalIgnoreCase))));
}
```

---

### IUserManager

**Purpose**: User account management and lookup

#### Key Methods

```csharp
public interface IUserManager
{
    User GetUserById(Guid id);
    User GetUserByName(string name);
    IEnumerable<User> Users { get; }
    
    Task<User> AuthenticateUser(string username, string password, ...);
}
```

#### Usage Example

```csharp
private async Task ProcessScrobbleAsync(Audio audio, Guid userId)
{
    var user = _userManager.GetUserById(userId);
    if (user == null)
    {
        _logger.Warn($"User {userId} not found");
        return;
    }
    
    var lastfmUser = GetLastfmUserForJellyfinUser(user);
    if (lastfmUser == null)
    {
        _logger.Debug($"No Last.fm user configured for {user.Name}");
        return;
    }
    
    await _apiClient.ScrobbleAsync(audio, lastfmUser).ConfigureAwait(false);
}
```

---

### IHttpClient

**Purpose**: HTTP requests to external APIs (e.g., Last.fm)

⚠️ **Critical**: Emby uses `IHttpClient` (not `IHttpClientFactory` from .NET)

#### HTTP Request Options

```csharp
public class HttpRequestOptions
{
    public string Url { get; set; }
    public string RequestContent { get; set; }
    public string RequestContentType { get; set; }
    public string AcceptHeader { get; set; }
    public Dictionary<string, string> RequestHeaders { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public int TimeoutMs { get; set; }
    public bool EnableHttpCompression { get; set; }
}
```

#### GET Request

```csharp
public async Task<T> GetAsync<T>(string url)
{
    var options = new HttpRequestOptions
    {
        Url = url,
        CancellationToken = CancellationToken.None,
        TimeoutMs = 30000,
        EnableHttpCompression = true
    };
    
    using (var response = await _httpClient.GetResponse(options).ConfigureAwait(false))
    {
        using (var stream = response.Content)
        using (var reader = new StreamReader(stream))
        {
            var json = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
```

#### POST Request

```csharp
public async Task<T> PostAsync<T>(string url, Dictionary<string, string> data)
{
    var content = new FormUrlEncodedContent(data);
    var contentString = await content.ReadAsStringAsync().ConfigureAwait(false);
    
    var options = new HttpRequestOptions
    {
        Url = url,
        RequestContent = contentString,
        RequestContentType = "application/x-www-form-urlencoded",
        CancellationToken = CancellationToken.None,
        TimeoutMs = 30000
    };
    
    using (var response = await _httpClient.Post(options).ConfigureAwait(false))
    {
        using (var stream = response.Content)
        using (var reader = new StreamReader(stream))
        {
            var json = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
```

---

### ILogManager

**Purpose**: Logging infrastructure

⚠️ **Different from Jellyfin**: Uses `ILogManager` (not `ILoggerFactory`)

#### Usage Pattern

```csharp
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly ILogger _logger;
    
    public ServerEntryPoint(ILogManager logManager)
    {
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    private void LogOperations()
    {
        _logger.Debug("Debug message");
        _logger.Info("Info message");
        _logger.Warn("Warning message");
        _logger.Error("Error message");
        _logger.Fatal("Fatal message");
        
        try
        {
            // ... code
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Operation failed", ex);
        }
    }
}
```

**Log Levels**:
- `Debug()` - Verbose debugging info
- `Info()` - General information
- `Warn()` - Warnings
- `Error()` - Errors
- `Fatal()` - Critical errors
- `ErrorException(string message, Exception ex)` - Errors with stack trace

---

### IServerConfigurationManager

**Purpose**: Server configuration access

```csharp
public interface IServerConfigurationManager
{
    ServerConfiguration Configuration { get; }
    void SaveConfiguration();
    
    event EventHandler<ConfigurationUpdateEventArgs> ConfigurationUpdated;
}
```

#### Usage Example

```csharp
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly IServerConfigurationManager _configManager;
    
    public ServerEntryPoint(IServerConfigurationManager configManager)
    {
        _configManager = configManager;
    }
    
    private void CheckServerSettings()
    {
        var serverConfig = _configManager.Configuration;
        var serverUrl = serverConfig.LocalNetworkAddresses?.FirstOrDefault();
        
        _logger.Info($"Server URL: {serverUrl}");
    }
}
```

---

## Media Types

### Audio Class

```csharp
public class Audio : BaseItem
{
    public string[] Artists { get; set; }
    public string[] AlbumArtists { get; set; }
    public string Album { get; set; }
    public string Name { get; set; }  // Track title
    public long? RunTimeTicks { get; set; }  // Duration
    public int? IndexNumber { get; set; }  // Track number
    public int? ParentIndexNumber { get; set; }  // Disc number
    
    // Provider IDs
    public Dictionary<string, string> ProviderIds { get; set; }
    // e.g., ["MusicBrainzRecording"] = "mbid-value"
}
```

### Accessing Provider IDs

```csharp
private string GetMusicBrainzId(Audio audio)
{
    if (audio.ProviderIds == null) return null;
    
    return audio.ProviderIds.ContainsKey("MusicBrainzRecording")
        ? audio.ProviderIds["MusicBrainzRecording"]
        : null;
}
```

---

## Scheduled Tasks

### IScheduledTask Interface

```csharp
public interface IScheduledTask
{
    string Name { get; }
    string Key { get; }
    string Description { get; }
    string Category { get; }
    
    IEnumerable<TaskTriggerInfo> GetDefaultTriggers();
    Task Execute(CancellationToken cancellationToken, IProgress<double> progress);
}
```

### Task Implementation

```csharp
public class SyncLovedTracksTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;
    private readonly ILogger _logger;
    
    public SyncLovedTracksTask(
        ILibraryManager libraryManager,
        IUserDataManager userDataManager,
        IUserManager userManager,
        ILogManager logManager)
    {
        _libraryManager = libraryManager;
        _userDataManager = userDataManager;
        _userManager = userManager;
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    public string Name => "Sync Loved Tracks from Last.fm";
    public string Key => "LastfmSyncLovedTracks";
    public string Description => "Import loved tracks from Last.fm and mark as favorites";
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
        _logger.Info("Starting loved tracks sync");
        progress.Report(0);
        
        var users = _userManager.Users.Where(u => HasLastfmUser(u)).ToList();
        var totalUsers = users.Count;
        
        for (var i = 0; i < totalUsers; i++)
        {
            var user = users[i];
            await SyncUserLovedTracksAsync(user, cancellationToken).ConfigureAwait(false);
            progress.Report((i + 1.0) / totalUsers * 100);
        }
        
        _logger.Info("Loved tracks sync complete");
    }
    
    private async Task SyncUserLovedTracksAsync(User user, CancellationToken ct)
    {
        var lovedTracks = await _apiClient.GetLovedTracksAsync(user, ct).ConfigureAwait(false);
        
        foreach (var track in lovedTracks)
        {
            var audio = FindMatchingTrack(track.Artist, track.Name, user.Id);
            if (audio != null)
            {
                var userData = _userDataManager.GetUserData(user, audio);
                if (!userData.IsFavorite)
                {
                    userData.IsFavorite = true;
                    _userDataManager.SaveUserData(user.Id, audio, userData,
                        UserDataSaveReason.UpdateUserRating, ct);
                }
            }
        }
    }
}
```

---

## Comparison: Jellyfin vs Emby APIs

| Feature | Jellyfin | Emby |
|---------|----------|------|
| **HTTP Client** | `IHttpClientFactory.CreateClient()` | `IHttpClient` (direct) |
| **Logging** | `ILoggerFactory.CreateLogger<T>()` | `ILogManager.GetLogger(name)` |
| **Log Methods** | `LogInformation()`, `LogError()` | `Info()`, `ErrorException()` |
| **Type Checks** | `is Audio` (pattern matching) | `GetType().Name == "Audio"` |
| **Nullable Refs** | `string?` (compiler-enforced) | `string` (runtime-only) |
| **Async Streams** | `IAsyncEnumerable<T>` | Not available |
| **Query Syntax** | Same | Same |
| **User Data** | Same | Same |
| **Events** | Same | Same |

---

**Related:**
- [emby-architecture.md](emby-architecture.md) - Plugin lifecycle
- [emby-patterns.md](emby-patterns.md) - .NET Framework 4.8 patterns
- [../api-cross-reference.instructions.md](../api-cross-reference.instructions.md) - Last.fm ↔ Emby mapping
        
        var audio = e.Item as Audio;
        var playedTicks = e.PlaybackPositionTicks ?? 0;
        var durationTicks = audio.RunTimeTicks ?? 0;
        
        // Check scrobble eligibility
        if (IsScrobbleEligible(durationTicks, playedTicks))
        {
            await ScrobbleAsync(audio, e.Users.FirstOrDefault()).ConfigureAwait(false);
        }
    }
}
    public override string Name => "Last.fm";
}

// 2. ServerEntryPoint.cs - Event handling
public class ServerEntryPoint : IServerEntryPoint
{
    public Task RunAsync() { /* Setup events */ }
    public void Dispose() { /* Cleanup */ }
}
```

## Available Services (Constructor Injection)

- `ISessionManager` - Playback events
- `IUserDataManager` - User data (favorites, play count)
- `ILibraryManager` - Library access
- `IHttpClient` - HTTP requests (NOTE: Different from Jellyfin's `IHttpClientFactory`)
- `ILogManager` - Logging

## TODO: Comprehensive Documentation

- [ ] Complete API reference
- [ ] Event system differences
- [ ] Configuration patterns
- [ ] .NET Framework 4.8 specific patterns
- [ ] Plugin packaging for Emby

---

**Related:**
- [jellyfin-architecture.md](../jellyfin-architecture.md) - Similar patterns
- [emby-architecture.md](emby-architecture.md) - Emby-specific architecture
