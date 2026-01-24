# Security Patterns

## Password Handling

```csharp
// ❌ NEVER store passwords
public class LastfmUser
{
    public string Password { get; set; }  // DON'T DO THIS
}

// ✅ Exchange password for permanent session key (one-time)
public async Task AuthenticateUser(string username, string password)
{
    var response = await _apiClient.RequestSession(username, password);
    
    // Store only the session key
    lastfmUser.SessionKey = response.SessionKey;
    lastfmUser.Username = username;  // Optional, non-sensitive
    // password is now out of scope, garbage collected
}

// After authentication, only use SessionKey
var result = await _apiClient.Scrobble(audio, lastfmUser);
```

**Key Facts**:
- Last.fm session keys don't expire (unless revoked by user)
- Never store, log, or transmit passwords
- Session keys should be treated as sensitive as passwords

---

## HTTP Security

```csharp
// ✅ Always use HttpClientFactory
var client = _httpClientFactory.CreateClient();
using var response = await client.GetAsync(url);

// ✅ Always use HTTPS (even though Last.fm uses HTTP)
var url = "https://ws.audioscrobbler.com/2.0/";

// ✅ Always set timeouts
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
using var response = await client.GetAsync(url, cts.Token);

// ✅ HttpClientFactory defaults to secure SSL/TLS validation
// DON'T disable certificate validation:
// handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;

// ✅ Use using() for automatic disposal
using (var response = await client.GetAsync(url))
{
    var content = await response.Content.ReadAsStringAsync();
}
```

---

## Configuration Security

```csharp
// ✅ Never log sensitive data
_logger.LogInformation("User {0} configured", user.Username);      // OK
_logger.LogDebug("Session key: {0}", sessionKey);                  // ❌ DON'T

// ✅ Sensitive data stays in plugin config XML only
var config = Plugin.Instance.PluginConfiguration;
config.LastfmUsers[0].SessionKey = secureKey;
// Jellyfin encrypts plugin config on disk

// ✅ Clear sensitive data on auth failure
if (response.IsError())
{
    lastfmUser.SessionKey = null;  // Clear invalid key
}

// ✅ Validate user input before using
if (string.IsNullOrWhiteSpace(username))
{
    _logger.LogWarning("Invalid username provided");
    return;
}
```

---

## JSON Deserialization Security

```csharp
// ✅ Use JsonSerializerOptions with safety defaults
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    // Don't allow arbitrary types (prevents XXE/deserialization attacks)
    TypeInfoResolver = JsonSerializerContext.Default
};

// ✅ Catch JSON parsing errors
try
{
    var response = await content.ReadFromJsonAsync<ScrobbleResponse>(options);
    if (response.IsError())
    {
        _logger.LogWarning("API error: {0}", response.Error);
    }
}
catch (JsonException ex)
{
    _logger.LogError(ex, "Malformed JSON response from Last.fm");
    // Don't propagate - API might have changed
}
```

---

## API Key Management

```csharp
// ✅ Store API key in environment or config (NOT in code)
public class LastfmApiClient
{
    private readonly string _apiKey = Environment.GetEnvironmentVariable("LASTFM_API_KEY");
    
    // OR from Jellyfin plugin config:
    private readonly string _apiKey = Plugin.Instance.PluginConfiguration.ApiKey;
}

// ❌ NEVER hardcode API keys
private const string API_KEY = "xxxxxxxxxxxxx";  // DON'T

// ✅ Never log API keys
_logger.LogInformation("Using Last.fm API");  // OK
_logger.LogDebug("API Key: {0}", _apiKey);    // ❌ DON'T
```

---

## Input Validation

```csharp
// ✅ Validate before use
public async Task Scrobble(string artist, string track, LastfmUser user)
{
    if (string.IsNullOrWhiteSpace(artist))
    {
        _logger.LogWarning("Invalid artist name");
        return;
    }
    
    if (string.IsNullOrWhiteSpace(track))
    {
        _logger.LogWarning("Invalid track name");
        return;
    }
    
    if (string.IsNullOrEmpty(user.SessionKey))
    {
        _logger.LogWarning("No session key for user {0}", user.Username);
        return;
    }
    
    // Now safe to use values
    var request = new ScrobbleRequest
    {
        Artist = artist,
        Track = track,
        SessionKey = user.SessionKey
    };
}

// ✅ Range validation
if (playedSeconds < 0 || playedSeconds > durationSeconds)
{
    return;  // Invalid playtime
}
```

---

## SQL Injection / Command Injection

Not applicable to this plugin (no database or system commands), but:

```csharp
// ✅ Never concatenate user input into commands/queries
var artistName = audio.Artists?.FirstOrDefault() ?? "Unknown";

// Use parameterized queries/requests instead:
var request = new ScrobbleRequest { Artist = artistName };

// ❌ WRONG: String concatenation
var payload = "artist=" + artistName;  // Vulnerable if artist contains &
```

---

## File & Resource Access

```csharp
// ✅ Use Jellyfin's application paths
var configPath = _applicationPaths.PluginConfigurationsPath;
var dataPath = _applicationPaths.PluginsPath;

// ✅ Always validate file paths
var fullPath = Path.Combine(configPath, filename);
if (!fullPath.StartsWith(configPath))
{
    throw new InvalidOperationException("Path traversal attempt");
}

// ✅ Use Path.Combine for cross-platform safety
var filePath = Path.Combine(_dataPath, "cache.json");  // Safe
var filePath2 = _dataPath + "/cache.json";             // Platform-specific
```

---

## Exception Handling & Information Disclosure

```csharp
// ✅ Don't expose internal details in error messages
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Failed to reach Last.fm");  // Safe, logged
    // Don't return exception details to user
}

// ❌ WRONG: Exposing stack traces
return Json(new { error = ex.StackTrace });  // Reveals internals

// ✅ Log full details for debugging, show generic message to user
_logger.LogError(ex, "Detailed error info");
return Json(new { error = "Service temporarily unavailable" });
```

---

**Related**:
- [lastfm-api-instructions.md](lastfm-api-instructions.md) - API security
- [csharp-patterns.md](csharp-patterns.md) - General patterns
