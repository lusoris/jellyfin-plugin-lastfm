---
applyTo: "**/Emby.*/**/*.cs"
description: Emby-specific code patterns and best practices
---

# Emby Code Patterns

Complete guide to .NET Framework 4.8 patterns and Emby-specific best practices.

## .NET Framework 4.8 Limitations

### 1. Pattern Matching

❌ **Not Available**: Modern C# pattern matching

```csharp
// ❌ DON'T: Pattern matching (C# 7+)
if (item is Audio audio)
{
    Process(audio);
}

// ✅ DO: Type checking + casting
if (item != null && item.GetType().Name == "Audio")
{
    var audio = item as Audio;
    if (audio != null)
    {
        Process(audio);
    }
}
```

### 2. Nullable Reference Types

❌ **Not Available**: Nullable reference types (C# 8+)

```csharp
// ❌ DON'T: Nullable annotations
public string? GetArtist(Audio? audio)
{
    return audio?.Artists?.FirstOrDefault();
}

// ✅ DO: Runtime null checks
public string GetArtist(Audio audio)
{
    if (audio == null) return null;
    if (audio.Artists == null || audio.Artists.Length == 0) return null;
    return audio.Artists[0];
}

// ✅ BETTER: Guard clauses
public string GetArtist(Audio audio)
{
    if (audio?.Artists == null || audio.Artists.Length == 0)
        return "Unknown Artist";
    
    return audio.Artists[0];
}
```

### 3. Async/Await Limitations

⚠️ **Always use** `ConfigureAwait(false)` in library code

```csharp
// ❌ DON'T: Missing ConfigureAwait
public async Task<string> GetDataAsync()
{
    var result = await _httpClient.GetAsync(url);
    return result.Content;
}

// ✅ DO: ConfigureAwait(false)
public async Task<string> GetDataAsync()
{
    var result = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return result.Content;
}

// ✅ DO: Nested async calls
public async Task ProcessDataAsync()
{
    var data = await GetDataAsync().ConfigureAwait(false);
    var parsed = await ParseDataAsync(data).ConfigureAwait(false);
    await SaveDataAsync(parsed).ConfigureAwait(false);
}
```

**Why ConfigureAwait(false)?**
- Avoids deadlocks in ASP.NET and WinForms
- Improves performance by not capturing SynchronizationContext
- Standard practice for library code

### 4. LINQ Limitations

⚠️ **Limited**: No `Chunk()`, `DistinctBy()`, `MaxBy()`, `MinBy()`

```csharp
// ❌ DON'T: Modern LINQ (.NET 6+)
var chunks = items.Chunk(10);
var distinct = items.DistinctBy(x => x.Id);
var max = items.MaxBy(x => x.Timestamp);

// ✅ DO: Manual chunking
public static IEnumerable<List<T>> ChunkBy<T>(IEnumerable<T> source, int chunkSize)
{
    var list = source.ToList();
    for (var i = 0; i < list.Count; i += chunkSize)
    {
        yield return list.Skip(i).Take(chunkSize).ToList();
    }
}

// ✅ DO: GroupBy for distinct
var distinct = items
    .GroupBy(x => x.Id)
    .Select(g => g.First());

// ✅ DO: OrderBy + FirstOrDefault for MaxBy
var max = items
    .OrderByDescending(x => x.Timestamp)
    .FirstOrDefault();
```

---

## HTTP Client Patterns

### IHttpClient Usage

⚠️ **Critical**: Emby uses `IHttpClient`, not `IHttpClientFactory`

```csharp
public class LastfmApiClient
{
    private readonly IHttpClient _httpClient;
    private readonly ILogger _logger;
    
    public LastfmApiClient(IHttpClient httpClient, ILogManager logManager)
    {
        _httpClient = httpClient;
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    public async Task<T> GetAsync<T>(string url)
    {
        var options = new HttpRequestOptions
        {
            Url = url,
            CancellationToken = CancellationToken.None,
            TimeoutMs = 30000,
            EnableHttpCompression = true,
            AcceptHeader = "application/json"
        };
        
        try
        {
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
        catch (HttpException ex)
        {
            _logger.ErrorException($"HTTP request failed: {url}", ex);
            throw;
        }
    }
    
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
        
        try
        {
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
        catch (HttpException ex)
        {
            _logger.ErrorException($"HTTP POST failed: {url}", ex);
            throw;
        }
    }
}
```

### Disposing Resources

⚠️ **Important**: Always dispose streams manually (no `using` declarations in .NET Framework 4.8)

```csharp
// ❌ DON'T: using declarations (C# 8+)
public async Task<string> ReadAsync()
{
    using var stream = GetStream();
    using var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync();
}

// ✅ DO: using statements
public async Task<string> ReadAsync()
{
    using (var stream = GetStream())
    {
        using (var reader = new StreamReader(stream))
        {
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
```

---

## Logging Patterns

### ILogManager vs ILoggerFactory

```csharp
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly ILogger _logger;
    
    // Emby: ILogManager
    public ServerEntryPoint(ILogManager logManager)
    {
        _logger = logManager.GetLogger(GetType().Name);
    }
    
    private void LogExamples()
    {
        // Verbose logging
        _logger.Debug("Detailed debug info");
        
        // Information
        _logger.Info("Operation completed");
        _logger.Info("Scrobbling: {0} - {1}", artist, track);
        
        // Warnings
        _logger.Warn("No MusicBrainz ID found");
        
        // Errors
        _logger.Error("Scrobble failed");
        _logger.ErrorException("Detailed error message", exception);
        
        // Critical errors
        _logger.Fatal("Plugin initialization failed");
    }
}
```

**Method Comparison**:

| Jellyfin | Emby |
|----------|------|
| `LogDebug()` | `Debug()` |
| `LogInformation()` | `Info()` |
| `LogWarning()` | `Warn()` |
| `LogError()` | `Error()` |
| `LogError(ex, "message")` | `ErrorException("message", ex)` |
| `LogCritical()` | `Fatal()` |

---

## Error Handling Patterns

### Exception Handling

```csharp
public async Task<bool> ScrobbleTrackAsync(Audio audio, User user)
{
    try
    {
        // Validate input
        if (audio == null)
        {
            _logger.Warn("Audio is null, cannot scrobble");
            return false;
        }
        
        if (user == null)
        {
            _logger.Warn("User is null, cannot scrobble");
            return false;
        }
        
        // Perform operation
        await _apiClient.ScrobbleAsync(audio, user).ConfigureAwait(false);
        _logger.Info($"Scrobbled: {audio.Name}");
        return true;
    }
    catch (HttpException ex)
    {
        _logger.ErrorException($"HTTP error during scrobble: {audio.Name}", ex);
        return false;
    }
    catch (Exception ex)
    {
        _logger.ErrorException($"Unexpected error during scrobble: {audio.Name}", ex);
        return false;
    }
}
```

### Retry Logic

```csharp
public async Task<T> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (HttpException ex) when (attempt < maxRetries)
        {
            _logger.Warn($"Attempt {attempt} failed, retrying... {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))).ConfigureAwait(false);
        }
    }
    
    // Final attempt throws
    return await operation().ConfigureAwait(false);
}

// Usage
var result = await RetryAsync(() => _apiClient.GetLovedTracksAsync(user)).ConfigureAwait(false);
```

---

## Event Handler Patterns

### async void Event Handlers

✅ **Correct**: Event handlers MUST be `async void`

```csharp
private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    try
    {
        if (e.Item == null || e.Item.GetType().Name != "Audio") return;
        
        var audio = e.Item as Audio;
        await ProcessScrobbleAsync(audio, e.Users.FirstOrDefault()).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.ErrorException("Error in PlaybackStopped handler", ex);
    }
}

private async void OnUserDataSaved(object sender, UserDataSaveEventArgs e)
{
    try
    {
        if (e.SaveReason != UserDataSaveReason.UpdateUserRating) return;
        if (e.Item == null || e.Item.GetType().Name != "Audio") return;
        
        var audio = e.Item as Audio;
        await ProcessFavoriteChangeAsync(audio, e.UserId, e.UserData.IsFavorite).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.ErrorException("Error in UserDataSaved handler", ex);
    }
}
```

**Why async void?**
- Event handlers have `void` return type in .NET events
- `async void` is the ONLY way to use await in event handlers
- Always wrap in try-catch to prevent unhandled exceptions

---

## Memory Management

### String Interning

```csharp
private static readonly string UnknownArtist = "Unknown Artist";
private static readonly string UnknownAlbum = "Unknown Album";

public string GetArtistName(Audio audio)
{
    if (audio?.Artists == null || audio.Artists.Length == 0)
        return UnknownArtist;  // Reuse same instance
    
    var artist = audio.Artists[0];
    
    // Intern common strings to save memory
    if (artist.Length < 100)
        return string.Intern(artist);
    
    return artist;
}
```

### Collection Initialization

```csharp
// ❌ DON'T: Collection expressions (C# 12)
List<string> items = ["item1", "item2"];

// ✅ DO: Collection initializer
var items = new List<string> { "item1", "item2" };

// ✅ DO: Array initialization
var items = new[] { "item1", "item2" };
```

---

## Concurrency Patterns

### Thread-Safe Collections

```csharp
using System.Collections.Concurrent;

public class ScrobbleQueue
{
    private readonly ConcurrentQueue<Scrobble> _queue = new ConcurrentQueue<Scrobble>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    
    public void Enqueue(Scrobble scrobble)
    {
        _queue.Enqueue(scrobble);
    }
    
    public async Task<bool> TryDequeueAsync(out Scrobble scrobble)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return _queue.TryDequeue(out scrobble);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Preventing Duplicate Scrobbles

```csharp
private readonly ConcurrentDictionary<string, DateTime> _recentScrobbles 
    = new ConcurrentDictionary<string, DateTime>();
private readonly TimeSpan _duplicateWindow = TimeSpan.FromSeconds(15);

private bool IsDuplicateScrobble(string artist, string track)
{
    var key = $"{artist}|{track}".ToLowerInvariant();
    var now = DateTime.UtcNow;
    
    if (_recentScrobbles.TryGetValue(key, out var lastScrobble))
    {
        if (now - lastScrobble < _duplicateWindow)
        {
            _logger.Debug($"Duplicate scrobble blocked: {artist} - {track}");
            return true;
        }
    }
    
    _recentScrobbles.AddOrUpdate(key, now, (k, v) => now);
    
    // Cleanup old entries
    var cutoff = now - TimeSpan.FromMinutes(5);
    var oldKeys = _recentScrobbles
        .Where(kvp => kvp.Value < cutoff)
        .Select(kvp => kvp.Key)
        .ToList();
    
    foreach (var oldKey in oldKeys)
    {
        _recentScrobbles.TryRemove(oldKey, out _);
    }
    
    return false;
}
```

---

## Configuration Patterns

### Plugin Configuration

```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    public LastfmUser[] LastfmUsers { get; set; }
    
    public PluginConfiguration()
    {
        LastfmUsers = Array.Empty<LastfmUser>();
    }
}

// Access configuration
public class ServerEntryPoint : IServerEntryPoint
{
    private PluginConfiguration Config => Plugin.Instance.Configuration;
    
    private void LoadUsers()
    {
        var users = Config.LastfmUsers;
        _logger.Info($"Loaded {users.Length} Last.fm users");
    }
    
    private void SaveUser(LastfmUser user)
    {
        var users = Config.LastfmUsers.ToList();
        users.Add(user);
        Config.LastfmUsers = users.ToArray();
        
        Plugin.Instance.SaveConfiguration();
        _logger.Info("Configuration saved");
    }
}
```

---

## Query Patterns

### Library Queries

```csharp
public async Task<List<Audio>> FindTracksAsync(string artist, string album, Guid userId)
{
    var user = _userManager.GetUserById(userId);
    
    var query = new InternalItemsQuery(user)
    {
        IncludeItemTypes = new[] { "Audio" },
        Recursive = true,
        SearchTerm = artist,  // Pre-filter
        Limit = 1000
    };
    
    var items = _libraryManager.GetItemList(query);
    
    // Manual filtering (LINQ)
    return items
        .OfType<Audio>()
        .Where(a =>
            a.Artists != null &&
            a.Artists.Any(art => string.Equals(art, artist, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(album) || string.Equals(a.Album, album, StringComparison.OrdinalIgnoreCase)))
        .ToList();
}
```

### Batch Operations

```csharp
public async Task<Dictionary<string, Audio>> FindTracksByMusicBrainzIdsAsync(
    IEnumerable<string> mbids, Guid userId)
{
    var result = new Dictionary<string, Audio>();
    var user = _userManager.GetUserById(userId);
    
    // Process in batches of 50
    var batches = ChunkBy(mbids, 50);
    
    foreach (var batch in batches)
    {
        foreach (var mbid in batch)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { "Audio" },
                Recursive = true,
                HasAnyProviderId = new Dictionary<string, string>
                {
                    ["MusicBrainzRecording"] = mbid
                }
            };
            
            var items = _libraryManager.GetItemList(query);
            var audio = items.OfType<Audio>().FirstOrDefault();
            
            if (audio != null)
            {
                result[mbid] = audio;
            }
        }
        
        // Rate limiting
        await Task.Delay(100).ConfigureAwait(false);
    }
    
    return result;
}
```

---

## Comparison: Jellyfin vs Emby Patterns

| Pattern | Jellyfin (.NET 9.0) | Emby (.NET Framework 4.8) |
|---------|---------------------|---------------------------|
| **Pattern Matching** | `if (item is Audio audio)` | `if (item.GetType().Name == "Audio")` |
| **Nullable Types** | `string?` | Manual null checks |
| **ConfigureAwait** | Optional | **Required** |
| **LINQ** | Full .NET 9 | Limited to .NET Framework 4.8 |
| **HTTP Client** | `IHttpClientFactory` | `IHttpClient` |
| **Logging** | `ILogger<T>` | `ILogManager.GetLogger(name)` |
| **using** | Declarations & statements | Statements only |
| **Collection Init** | `["item1"]` | `new[] { "item1" }` |

---

**Related:**
- [emby-architecture.md](emby-architecture.md) - Plugin lifecycle
- [emby-api.md](emby-api.md) - API reference
- [../csharp/csharp-security.md](../csharp/csharp-security.md) - Security patterns

### Async Patterns

```csharp
// .NET Framework 4.8 supports Task-based async, but:
// - ConfigureAwait(false) is more critical
// - ValueTask not available
// - Some LINQ async methods missing

public async Task<List<Track>> GetTracksAsync()
{
    var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return ParseTracks(response);
}
```

## TODO: Complete Patterns

- [ ] Configuration persistence
- [ ] Event handler patterns
- [ ] Error handling
- [ ] JSON serialization (System.Text.Json vs Newtonsoft)
- [ ] Dependency injection
- [ ] Testing patterns

---

**Related:**
- [../csharp/csharp-patterns.md](../csharp/csharp-patterns.md) - General C# patterns
- [emby-api.md](emby-api.md) - API reference
