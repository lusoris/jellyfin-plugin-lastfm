# Security Patterns & Strict Mode

## Project Security Settings

### Required .csproj Configuration

```xml
<PropertyGroup>
  <!-- Enable all .NET analyzers -->
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  
  <!-- Enforce code style in build -->
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  
  <!-- STRICT: Treat all warnings as errors -->
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningLevel>9999</WarningLevel>
  
  <!-- Enable nullable reference types -->
  <Nullable>enable</Nullable>
</PropertyGroup>
```

### Suppressing Warnings (Document Everything!)

```xml
<!--
  Suppressed warnings (ALWAYS document why):
  CA1002: Don't expose List<T> - not practical for JSON DTOs
  CA5351: MD5 is broken - Required by Last.fm API, not used for security
-->
<NoWarn>CA1002;CA5351</NoWarn>
```

**Rule**: Never suppress a warning without a comment explaining why.

---

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
// ✅ ALWAYS use HttpClientFactory (connection pooling, DNS refresh)
public sealed class LastfmApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, ct);
        // ...
    }
}

// ✅ ALWAYS use HTTPS
private const string BaseUrl = "https://ws.audioscrobbler.com/2.0/";

// ✅ ALWAYS set timeouts
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
using var response = await client.GetAsync(url, cts.Token);

// ✅ ALWAYS dispose responses
using var response = await client.GetAsync(url);
using var content = await response.Content.ReadAsStreamAsync();

// ❌ NEVER disable certificate validation
// handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

// ❌ NEVER create HttpClient directly (causes socket exhaustion)
var client = new HttpClient();  // DON'T
```

---

## Configuration Security

```csharp
// ✅ NEVER log sensitive data
_logger.LogInformation("User {Username} configured", user.Username);  // OK
_logger.LogDebug("Session key: {Key}", sessionKey);                   // ❌ DON'T

// ✅ Sensitive data stays in plugin config XML only
var config = Plugin.Instance.PluginConfiguration;
// Jellyfin encrypts plugin config on disk

// ✅ Clear sensitive data on auth failure
if (response.IsError)
{
    lastfmUser.SessionKey = null;  // Clear invalid key
}

// ✅ Validate ALL user input
if (string.IsNullOrWhiteSpace(username))
{
    _logger.LogWarning("Invalid username provided");
    return BadRequest("Username is required");
}
```

---

## JSON Deserialization Security

```csharp
// ✅ Use System.Text.Json with safe defaults
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    // DON'T set: TypeInfoResolver = custom (allows arbitrary types)
};

// ✅ ALWAYS catch JSON parsing errors
try
{
    var response = await JsonSerializer.DeserializeAsync<ScrobbleResponse>(
        stream, JsonOptions, cancellationToken);
    
    if (response?.IsError == true)
    {
        _logger.LogWarning("API error: {Error}", response.Error);
    }
}
catch (JsonException ex)
{
    _logger.LogError(ex, "Malformed JSON response from Last.fm");
    // Don't propagate - API might have changed
    return null;
}

// ❌ NEVER use Newtonsoft.Json TypeNameHandling
// settings.TypeNameHandling = TypeNameHandling.Auto;  // RCE vulnerability!
```

---

## API Key Management

```csharp
// ✅ Store API key in plugin config (not in code)
public sealed class LastfmApiClient
{
    private readonly string _apiKey;
    
    public LastfmApiClient(PluginConfiguration config)
    {
        _apiKey = config.ApiKey;
    }
}

// ❌ NEVER hardcode API keys
private const string ApiKey = "xxxxxxxxxxxxx";  // DON'T

// ❌ NEVER log API keys
_logger.LogDebug("API Key: {Key}", _apiKey);  // DON'T

// ✅ Mask sensitive data in debug output
_logger.LogDebug("API Key configured: {Masked}", 
    string.IsNullOrEmpty(_apiKey) ? "NO" : "YES");
```

---

## Input Validation

```csharp
// ✅ Validate BEFORE use (fail fast)
public async Task<bool> Scrobble(string artist, string track, LastfmUser user)
{
    // Null/empty checks
    ArgumentException.ThrowIfNullOrWhiteSpace(artist);
    ArgumentException.ThrowIfNullOrWhiteSpace(track);
    ArgumentNullException.ThrowIfNull(user);
    
    if (string.IsNullOrEmpty(user.SessionKey))
    {
        _logger.LogWarning("No session key for user {Username}", user.Username);
        return false;
    }
    
    // Range validation
    if (playedSeconds < 0)
    {
        _logger.LogWarning("Invalid played time: {Seconds}", playedSeconds);
        return false;
    }
    
    // Now safe to use values
    var request = new ScrobbleRequest
    {
        Artist = artist.Trim(),
        Track = track.Trim(),
        SessionKey = user.SessionKey
    };
}
```

---

## URL & Path Security

```csharp
// ✅ URL-encode user input
var encodedArtist = Uri.EscapeDataString(artist);
var url = $"{BaseUrl}?artist={encodedArtist}";

// ✅ Use UriBuilder for complex URLs
var builder = new UriBuilder(BaseUrl)
{
    Query = $"method=track.scrobble&artist={Uri.EscapeDataString(artist)}"
};

// ✅ Use Jellyfin's application paths
var configPath = _applicationPaths.PluginConfigurationsPath;
var dataPath = _applicationPaths.PluginsPath;

// ✅ ALWAYS validate file paths (prevent path traversal)
var fullPath = Path.GetFullPath(Path.Combine(configPath, filename));
if (!fullPath.StartsWith(configPath, StringComparison.OrdinalIgnoreCase))
{
    throw new SecurityException("Path traversal attempt detected");
}

// ✅ Use Path.Combine (cross-platform safe)
var filePath = Path.Combine(_dataPath, "cache.json");  // Safe

// ❌ DON'T use string concatenation for paths
var filePath = _dataPath + "/" + filename;  // Platform-specific, unsafe
```

---

## Exception Handling & Information Disclosure

```csharp
// ✅ Log full details internally, return generic message to user
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Failed to reach Last.fm API");
    return new ApiResponse { Error = "Service temporarily unavailable" };
}

// ❌ NEVER expose stack traces or internal details
return Json(new { error = ex.StackTrace });  // Reveals internals!
return Json(new { error = ex.Message });     // May reveal internals!

// ✅ Use structured error responses
public record ApiError(string Code, string Message);
return BadRequest(new ApiError("INVALID_INPUT", "Artist name is required"));
```

---

## MD5 Signature (Last.fm Specific)

```csharp
// Last.fm requires MD5 for API signatures (not for security!)
// CA5351 suppressed: MD5 required by Last.fm API, not used for security

public static string CreateSignature(SortedDictionary<string, string> parameters, string secret)
{
    var signatureBase = new StringBuilder();
    foreach (var (key, value) in parameters)
    {
        signatureBase.Append(key);
        signatureBase.Append(value);
    }
    signatureBase.Append(secret);
    
    var inputBytes = Encoding.UTF8.GetBytes(signatureBase.ToString());
    var hashBytes = MD5.HashData(inputBytes);
    
    // ⚠️ CRITICAL: Last.fm requires LOWERCASE hex
    return Convert.ToHexStringLower(hashBytes);
}
```

---

## Rate Limiting & DoS Prevention

```csharp
// ✅ Implement rate limiting for external APIs
public sealed class RateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private int _requestCount;
    private DateTime _windowStart;
    
    public async Task<bool> TryAcquireAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            if (now - _windowStart > _window)
            {
                _windowStart = now;
                _requestCount = 0;
            }
            
            if (_requestCount >= _maxRequests)
                return false;
            
            _requestCount++;
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

// ✅ Use exponential backoff for retries
var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
await Task.Delay(delay, cancellationToken);
```

---

## Secrets in Source Control

```bash
# ✅ Add to .gitignore
appsettings.Development.json
*.user
.env
secrets.json

# ✅ Use environment variables for local development
export LASTFM_API_KEY="your-key-here"

# ✅ Use Jellyfin's plugin config for production
# (automatically encrypted on disk)
```

---

## Security Checklist

Before committing code, verify:

- [ ] No passwords stored (only session keys)
- [ ] No API keys in source code
- [ ] All user input validated
- [ ] All HTTP responses disposed
- [ ] HTTPS used for all external calls
- [ ] No sensitive data in logs
- [ ] JSON deserialization catches exceptions
- [ ] File paths validated against traversal
- [ ] Error messages don't expose internals
- [ ] `TreatWarningsAsErrors` enabled in csproj
- [ ] All suppressed warnings documented

---

**Related**:
- [csharp-patterns.md](csharp-patterns.md) - General patterns
- [lastfm-api.instructions.md](lastfm-api.instructions.md) - API authentication
- [development-workflow.md](development-workflow.md) - Build process
