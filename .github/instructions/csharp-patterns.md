# C# Patterns & Best Practices

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

// ❌ WRONG: Task.Result causes deadlocks
var result = _apiClient.Scrobble(...).Result;

// ❌ WRONG: Direct HttpClient (doesn't pool)
var client = new HttpClient();
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

// Safe cast
var audio = e.Item as Audio;

// Null-conditional with coalescing
var duration = item.RunTimeTicks?.ToString() ?? "0";
```

---

## LINQ & Collections

```csharp
// Filtering with multiple conditions
var users = _userManager.GetUsers()
    .Where(u => u.Policy.IsAdministrator)
    .Where(u => !u.HasPassword)
    .ToList();

// FirstOrDefault with fallback
var firstSong = item.Artists.FirstOrDefault() ?? "Unknown";

// Any() for existence checks
if (config.LastfmUsers.Any(u => u.MediaBrowserUserId == userId))
{
    // User has config
}

// Select + Where chains
var configuredUsers = config.LastfmUsers
    .Where(u => u.Options.Scrobble)
    .Select(u => u.MediaBrowserUserId)
    .ToList();

// GroupBy for organization
var groupedByArtist = songs
    .GroupBy(s => s.Artist)
    .Select(g => new { Artist = g.Key, Count = g.Count() })
    .ToList();
```

---

## String Handling

```csharp
// String interpolation (preferred)
_logger.LogInformation("Scrobbling {0} by {1}", item.Name, artist);
_logger.LogInformation($"Scrobbling {item.Name} by {artist}");

// String concatenation (when needed)
var fullName = string.Concat(item.Artists.Select(a => $"{a}, "));

// null-safe string operations
var trimmed = artist?.Trim() ?? string.Empty;
```

---

## Logging

```csharp
_logger.LogDebug("Debug info: {0}", detailVar);           // Verbose
_logger.LogInformation("Starting scrobble");               // Normal
_logger.LogWarning("Rate limit approaching");              // Warning
_logger.LogError(ex, "Scrobble failed: {0}", user);       // Error

// Don't log sensitive data
_logger.LogInformation("User {0} configured", user.Username);  // ✅
_logger.LogDebug("Session key: {0}", sessionKey);              // ❌
```

---

## Dictionary & Collections

```csharp
// Dictionary initialization
var dict = new Dictionary<string, string>
{
    { "key1", "value1" },
    { "key2", "value2" }
};

// Safe dictionary operations
if (dict.TryGetValue("key", out var value))
{
    // Use value
}

// Null-coalescing for missing keys
var val = dict.ContainsKey("key") ? dict["key"] : null;

// Remove items safely
dict.Remove("key");
```

---

## Exception Handling

```csharp
// Catch specific exceptions
try
{
    var result = await _apiClient.Scrobble(item, user).ConfigureAwait(false);
}
catch (HttpRequestException ex)
{
    _logger.LogWarning(ex, "Network error");
    // Network issue - don't propagate, continue
}
catch (JsonException ex)
{
    _logger.LogError(ex, "JSON parsing failed");
    // API response malformed - skip this request
}

// Async void handlers need try-catch
private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    try
    {
        await _apiClient.Scrobble(item, user).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception in PlaybackStopped");
        // ConfigureAwait(false) will log exceptions
    }
}
```

---

## using Statements

```csharp
// Traditional using
using (var response = await client.GetAsync(url))
{
    var content = await response.Content.ReadAsStringAsync();
}

// Modern using (C# 8+)
using var response = await client.GetAsync(url);
var content = await response.Content.ReadAsStringAsync();
```

---

## Delegates & Actions

```csharp
// Event handler (async void is correct here)
private async void OnPlaybackStopped(object sender, EventArgs e)
{
    await Task.Delay(100).ConfigureAwait(false);
}

// Subscribe/unsubscribe
_sessionManager.PlaybackStopped += OnPlaybackStopped;
_sessionManager.PlaybackStopped -= OnPlaybackStopped;

// Func/Action for callbacks
Func<string, Task<bool>> validator = async (name) =>
{
    return await IsValidName(name);
};
```

---

**Related**:
- [csharp-security.md](csharp-security.md) - Security patterns
- [development-workflow.md](development-workflow.md) - Build & testing
