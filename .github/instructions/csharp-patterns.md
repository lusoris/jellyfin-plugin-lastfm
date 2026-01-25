# C# Patterns & Best Practices

## Class Design

```csharp
// ✅ ALWAYS seal non-inheritable classes (performance + clarity)
public sealed class ScrobbleService : IDisposable
{
    // Sealed classes enable devirtualization optimizations
}

// ✅ ALWAYS implement IDisposable for services with subscriptions
public sealed class PlaybackHandler : IDisposable
{
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Unsubscribe from events, release resources
    }
}
```

---

## LoggerMessage Source Generators (Performance)

```csharp
// ✅ Use [LoggerMessage] for high-performance logging (zero allocations)
public sealed partial class ScrobbleService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Scrobbling {TrackName} by {Artist}")]
    private partial void LogScrobble(string trackName, string artist);
    
    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit approaching: {Current}/{Max}")]
    private partial void LogRateLimitWarning(int current, int max);
    
    [LoggerMessage(Level = LogLevel.Error, Message = "Scrobble failed for user {Username}")]
    private partial void LogScrobbleError(Exception ex, string username);
}

// Usage (zero allocations, compile-time validation):
LogScrobble(track.Name, track.Artist);
LogScrobbleError(ex, user.Username);

// ❌ AVOID: String interpolation in hot paths (allocates)
_logger.LogInformation($"Scrobbling {track.Name}");  // Allocates string every call
```

**Requirements:**
- Class must be `partial`
- Method must be `partial`
- `Level` and `Message` are required

---

## Async/Await

```csharp
// ✅ CORRECT: ConfigureAwait(false) in library code
await _apiClient.Scrobble(item, user).ConfigureAwait(false);

// ✅ CORRECT: HttpClientFactory usage (reuses connections)
var client = _httpClientFactory.CreateClient();
using var response = await client.GetAsync(url);

// ✅ CORRECT: CancellationToken support
public async Task DoWork(CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await Task.Delay(100, cancellationToken);
}

// ✅ CORRECT: ValueTask for potentially-sync paths
public ValueTask<bool> TryGetCached(string key, out string? value)
{
    if (_cache.TryGetValue(key, out value))
        return ValueTask.FromResult(true);
    return new ValueTask<bool>(FetchAsync(key));
}

// ❌ WRONG: Task.Result causes deadlocks
var result = _apiClient.Scrobble(...).Result;

// ❌ WRONG: Direct HttpClient (doesn't pool)
var client = new HttpClient();
```

---

## Memory & Performance Patterns

```csharp
// ✅ Use Span<T> for stack-allocated buffers
Span<byte> buffer = stackalloc byte[256];
var bytesWritten = Encoding.UTF8.GetBytes(input, buffer);

// ✅ Use ArrayPool for large temporary buffers
var pool = ArrayPool<byte>.Shared;
var buffer = pool.Rent(4096);
try
{
    // Use buffer
}
finally
{
    pool.Return(buffer);
}

// ✅ Use string.Create for efficient string building
var result = string.Create(length, state, (span, s) =>
{
    // Write directly to span
});

// ✅ Prefer ReadOnlySpan<char> for string operations
public bool StartsWithIgnoreCase(ReadOnlySpan<char> value, ReadOnlySpan<char> prefix)
    => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

// ✅ Convert hex efficiently (.NET 5+)
var hexLower = Convert.ToHexStringLower(bytes);  // Last.fm requires lowercase!

// ❌ AVOID: Repeated string concatenation
string result = "";
foreach (var item in items)
    result += item;  // O(n²) allocations

// ✅ Use StringBuilder for multiple concatenations
var sb = new StringBuilder();
foreach (var item in items)
    sb.Append(item);
var result = sb.ToString();
```

---

## Null Coalescing & Pattern Matching

```csharp
// Modern null-coalescing
var artistName = item.Artists?.FirstOrDefault() ?? "Unknown";

// Null-coalescing assignment
lastfmUser.SessionKey ??= GetNewSessionKey();

// Pattern matching with negation
if (e.Item is not Audio) return;

// Pattern matching with extraction
if (e.Item is Audio audio && audio.RunTimeTicks is { } ticks)
{
    var seconds = ticks / TimeSpan.TicksPerSecond;
}

// Switch expression
var level = errorCount switch
{
    0 => LogLevel.Information,
    < 5 => LogLevel.Warning,
    _ => LogLevel.Error
};

// Null-conditional with coalescing
var duration = item.RunTimeTicks?.ToString() ?? "0";
```

---

## LINQ & Collections

```csharp
// ✅ Use specific collection types when size is known
var list = new List<string>(capacity: 10);

// ✅ Avoid LINQ in hot paths (allocates enumerators)
// For small collections, use foreach instead:
foreach (var user in users)
{
    if (user.IsEnabled)
        ProcessUser(user);
}

// ✅ Use Array.Find for simple searches
var user = Array.Find(users, u => u.Id == targetId);

// ✅ For larger datasets, LINQ is fine
var configuredUsers = config.LastfmUsers
    .Where(u => u.Options.Scrobble)
    .Select(u => u.MediaBrowserUserId)
    .ToList();

// ✅ Any() for existence checks (more efficient than Count() > 0)
if (config.LastfmUsers.Any(u => u.MediaBrowserUserId == userId))
{
    // User has config
}

// ✅ FirstOrDefault with fallback
var firstSong = item.Artists.FirstOrDefault() ?? "Unknown";
```

---

## String Handling

```csharp
// ✅ String interpolation (preferred for readability)
var message = $"Scrobbling {item.Name} by {artist}";

// ✅ Use StringComparison for case-insensitive comparisons
if (artist.Equals(other, StringComparison.OrdinalIgnoreCase))

// ✅ Use string.IsNullOrEmpty / IsNullOrWhiteSpace
if (string.IsNullOrWhiteSpace(artist))
    return;

// ✅ null-safe string operations
var trimmed = artist?.Trim() ?? string.Empty;

// ✅ String.Join for collections
var artists = string.Join(", ", item.Artists);
```

---

## Logging Best Practices

```csharp
// ✅ Structured logging with placeholders (not string interpolation)
_logger.LogInformation("Scrobbling {TrackName} by {Artist}", item.Name, artist);

// ✅ Use LoggerMessage for hot paths (see above)

// ✅ Include exception as first parameter
_logger.LogError(ex, "Scrobble failed for {User}", user.Username);

// ✅ Use appropriate log levels
_logger.LogDebug("...");        // Verbose debugging
_logger.LogInformation("...");  // Normal operation
_logger.LogWarning("...");      // Recoverable issues
_logger.LogError(ex, "...");    // Failures

// ❌ NEVER log sensitive data
_logger.LogDebug("Session key: {Key}", sessionKey);  // DON'T!
```

---

## Dictionary & Collections

```csharp
// ✅ Dictionary initialization
var dict = new Dictionary<string, string>
{
    ["key1"] = "value1",  // Indexer syntax (preferred)
    ["key2"] = "value2"
};

// ✅ TryGetValue pattern (single lookup)
if (dict.TryGetValue("key", out var value))
{
    // Use value
}

// ✅ GetValueOrDefault for nullable fallback
var val = dict.GetValueOrDefault("key");

// ✅ TryAdd for conditional insertion
dict.TryAdd("key", "value");  // Returns false if exists
```

---

## Exception Handling

```csharp
// ✅ Catch specific exceptions
try
{
    var result = await _apiClient.Scrobble(item, user).ConfigureAwait(false);
}
catch (HttpRequestException ex)
{
    _logger.LogWarning(ex, "Network error");
    // Network issue - queue for retry
}
catch (JsonException ex)
{
    _logger.LogError(ex, "JSON parsing failed");
    // API response malformed - skip this request
}

// ✅ Async void handlers MUST have try-catch
private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    try
    {
        await _apiClient.Scrobble(item, user).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception in PlaybackStopped");
    }
}
```

---

## using Statements & Disposal

```csharp
// ✅ Modern using declaration (C# 8+)
using var response = await client.GetAsync(url);
var content = await response.Content.ReadAsStringAsync();
// Disposed at end of scope

// ✅ IAsyncDisposable for async cleanup
await using var stream = await OpenAsync();

// ✅ Dispose pattern for unmanaged resources
public sealed class MyService : IDisposable
{
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Clean up managed resources
        _subscription?.Dispose();
        // Unsubscribe from events
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
    }
}
```

---

## Event Handlers

```csharp
// ✅ async void is CORRECT for event handlers
private async void OnPlaybackStopped(object sender, EventArgs e)
{
    try
    {
        await ProcessAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Handler error");
    }
}

// ✅ Subscribe in StartAsync, unsubscribe in StopAsync/Dispose
public Task StartAsync(CancellationToken ct)
{
    _sessionManager.PlaybackStopped += OnPlaybackStopped;
    return Task.CompletedTask;
}

public Task StopAsync(CancellationToken ct)
{
    _sessionManager.PlaybackStopped -= OnPlaybackStopped;
    return Task.CompletedTask;
}
```

---

**Related**:
- [csharp-security.md](csharp-security.md) - Security patterns
- [development-workflow.md](development-workflow.md) - Build & testing
- [ide-setup.md](ide-setup.md) - Development environment
