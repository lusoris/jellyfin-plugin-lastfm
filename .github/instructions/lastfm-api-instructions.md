# Last.fm API Integration - Instructions

## 1. API OVERVIEW

### Endpoints
- **Base URL**: `http://ws.audioscrobbler.com/2.0/?format=json`
- **Method**: POST (all requests)
- **Format**: JSON responses
- **Authentication**: API Key + MD5 signature

### Available Methods
- `auth.getMobileSession` - Get permanent session key
- `track.scrobble` - Submit track play
- `track.updateNowPlaying` - Send now playing info
- `user.getLovedTracks` - Fetch user's loved/favorited tracks
- `track.love` - Mark track as loved

---

## 2. AUTHENTICATION FLOW

### Session Key Generation (One-Time Setup)

```csharp
// User provides username + password
public async Task<MobileSessionResponse> RequestSession(string username, string password)
{
    var request = new MobileSessionRequest 
    { 
        Username = username, 
        Password = password,
        Method = "auth.getMobileSession"
    };
    return await Post<MobileSessionRequest, MobileSessionResponse>(request);
}

// Response contains:
// {
//   "session": {
//     "name": "username",
//     "key": "xxxxx-permanent-session-key-xxxxx",
//     "subscriber": 0
//   }
// }
```

**Important**: 
- ❌ NEVER store or log passwords
- ✅ Store `session.key` permanently (never expires unless revoked)
- ✅ Session key is user-specific, immutable

### MD5 Signature Generation

All requests require MD5 signature to prevent tampering:

```csharp
public static void AppendSignature(ref Dictionary<string, string> data)
{
    // Sort all parameters alphabetically
    var sorted = data.OrderBy(x => x.Key).ToList();
    
    // Build string: key1value1key2value2...keyNvalueN + API_SECRET
    var signString = string.Concat(sorted.Select(x => $"{x.Key}{x.Value}")) + API_SECRET;
    
    // MD5 hash
    using var md5 = System.Security.Cryptography.MD5.Create();
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signString));
    var signature = string.Concat(hash.Select(x => x.ToString("x2")));
    
    data["api_sig"] = signature;
}
```

**Parameters required in ALL requests**:
```csharp
{
    "api_key": "xxxxxxxxxxxxxxxx",      // From Last.fm app registration
    "format": "json",                    // Fixed
    "method": "track.scrobble",          // Varies by endpoint
    "sk": "session-key-from-auth",       // Permanent session key
    "api_sig": "md5-hash-here"           // Computed above
}
```

---

## 3. SCROBBLING (track.scrobble)

### Request Format
```csharp
public class ScrobbleRequest : BaseRequest
{
    public string Method => "track.scrobble";
    public string SessionKey { get; set; }        // From auth
    public string Artist { get; set; }            // Track artist
    public string Track { get; set; }             // Track title
    public string Album { get; set; }             // Optional
    public long Timestamp { get; set; }           // Unix epoch when played
    public int? Duration { get; set; }            // Optional: track length in seconds
}
```

### Scrobbling Rules (Last.fm Official)
**Track must satisfy ALL conditions**:
1. ✅ Duration ≥ 30 seconds
2. ✅ User played ≥ 4 minutes OR ≥ 50% of track (whichever comes first)
3. ✅ Not a duplicate within 15 seconds (same artist + track)

### Implementation Pattern
```csharp
public async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
{
    if (e.Item is not Audio audio) return;
    
    var config = Plugin.Instance.PluginConfiguration;
    var lastfmUser = config.LastfmUsers.FirstOrDefault(u => u.MediaBrowserUserId == e.UserId);
    if (lastfmUser?.Options.Scrobble != true) return;
    
    // Rule 1: Duration ≥ 30 seconds
    var durationSeconds = audio.RunTimeTicks.HasValue ? audio.RunTimeTicks.Value / 10_000_000 : 0;
    if (durationSeconds < 30)
    {
        _logger.LogDebug("Track too short: {0}s", durationSeconds);
        return;
    }
    
    // Rule 2: Played ≥ 4min OR ≥ 50%
    var playedSeconds = e.PlaybackPositionTicks / 10_000_000;
    var playedPercent = (double)playedSeconds / durationSeconds;
    var minTime = Math.Min(240, durationSeconds / 2);  // 4min or 50%
    
    if (playedSeconds < minTime)
    {
        _logger.LogDebug("Not played enough: {0}s / {1}s ({2}%)", 
            playedSeconds, durationSeconds, (int)(playedPercent * 100));
        return;
    }
    
    // Rule 3: Duplicate prevention (check last scrobble timestamp)
    var timeSinceLastScrobble = DateTime.UtcNow - _lastScrobbleTime;
    if (timeSinceLastScrobble.TotalSeconds < 15 && 
        _lastScrobbleArtist == audio.Artists?.FirstOrDefault() && 
        _lastScrobbleTrack == audio.Name)
    {
        _logger.LogDebug("Duplicate scrobble within 15 seconds");
        return;
    }
    
    // Submit scrobble
    try
    {
        var response = await _apiClient.Scrobble(audio, lastfmUser).ConfigureAwait(false);
        if (response.IsError())
        {
            _logger.LogWarning("Scrobble failed: {0}", response.Message);
            return;
        }
        
        _lastScrobbleTime = DateTime.UtcNow;
        _lastScrobbleArtist = audio.Artists?.FirstOrDefault();
        _lastScrobbleTrack = audio.Name;
        _logger.LogInformation("Scrobbled: {0} - {1}", audio.Artists?.FirstOrDefault(), audio.Name);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Scrobble exception");
    }
}
```

---

## 4. NOW PLAYING (track.updateNowPlaying)

### Request Format
```csharp
public class NowPlayingRequest : BaseRequest
{
    public string Method => "track.updateNowPlaying";
    public string SessionKey { get; set; }
    public string Artist { get; set; }
    public string Track { get; set; }
    public string Album { get; set; }
}
```

### Usage
```csharp
public async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
{
    if (e.Item is not Audio audio) return;
    
    var config = Plugin.Instance.PluginConfiguration;
    var lastfmUser = config.LastfmUsers.FirstOrDefault(u => u.MediaBrowserUserId == e.Users?.FirstOrDefault()?.Id);
    if (lastfmUser == null) return;
    
    try
    {
        await _apiClient.NowPlaying(audio, lastfmUser).ConfigureAwait(false);
        _logger.LogDebug("Now playing: {0}", audio.Name);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Now playing update failed");
    }
}
```

---

## 5. LOVED TRACKS (user.getLovedTracks)

### Request Format
```csharp
public class GetLovedTracksRequest : BaseRequest
{
    public string Method => "user.getLovedTracks";
    public string SessionKey { get; set; }
    public int Limit { get; set; } = 1000;        // Max per page
    public int Page { get; set; } = 1;
}
```

### Response Format
```json
{
  "lovedtracks": {
    "track": [
      {
        "artist": {
          "name": "Artist Name",
          "mbid": "musicbrainz-id"
        },
        "name": "Track Title",
        "mbid": "track-mbid",
        "url": "last.fm-url"
      }
    ],
    "@attr": {
      "user": "username",
      "totalPages": "5",
      "page": "1",
      "perPage": "1000",
      "total": "4321"
    }
  }
}
```

### Pagination Pattern
```csharp
public async Task<List<Track>> GetAllLovedTracks(LastfmUser user)
{
    var allTracks = new List<Track>();
    int page = 1;
    int totalPages = 1;
    
    while (page <= totalPages)
    {
        var request = new GetLovedTracksRequest 
        { 
            SessionKey = user.SessionKey,
            Page = page,
            Limit = 1000
        };
        
        var response = await Post<GetLovedTracksRequest, LovedTracksResponse>(request);
        if (response.IsError())
        {
            _logger.LogError("Failed to fetch loved tracks page {0}: {1}", page, response.Message);
            break;
        }
        
        allTracks.AddRange(response.LovedTracks);
        totalPages = response.Attributes.TotalPages;
        page++;
        
        // Respect rate limits (1 request per second recommended)
        await Task.Delay(1000).ConfigureAwait(false);
    }
    
    return allTracks;
}
```

---

## 6. LOVE TRACK (track.love)

### Request Format
```csharp
public class TrackLoveRequest : BaseRequest
{
    public string Method => "track.love";
    public string SessionKey { get; set; }
    public string Artist { get; set; }
    public string Track { get; set; }
}
```

### Usage (When User Marks as Favorite in Jellyfin)
```csharp
private async void UserDataSaved(object sender, UserDataSaveEventArgs e)
{
    if (e.Item is not Audio audio) return;
    
    var config = Plugin.Instance.PluginConfiguration;
    var lastfmUser = config.LastfmUsers.FirstOrDefault(u => u.MediaBrowserUserId == e.UserId);
    if (lastfmUser?.Options.Scrobble != true) return;
    
    // Only handle favorite toggles
    if (e.SaveReason != UserDataSaveReason.UpdateUserRating) return;
    
    try
    {
        if (e.UserData.IsFavorite)
        {
            // User marked as favorite - love on Last.fm
            var response = await _apiClient.LoveTrack(audio, lastfmUser).ConfigureAwait(false);
            if (!response.IsError())
            {
                _logger.LogInformation("Loved on Last.fm: {0}", audio.Name);
            }
        }
        // Note: Last.fm doesn't have "unlove" endpoint, users must do it on Last.fm site
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Love track failed");
    }
}
```

---

## 7. ERROR HANDLING

### Last.fm Error Response Format
```json
{
  "error": 4,
  "message": "User not found"
}
```

### Common Error Codes
| Code | Meaning | Handling |
|------|---------|----------|
| 2 | Invalid service | Check method name spelling |
| 3 | Auth failed | Session key expired or invalid |
| 4 | Object not found | User doesn't exist |
| 5 | Invalid params | Missing required field |
| 6 | Invalid resource | Track/artist not found |
| 7 | Op failed | Server error, retry with backoff |
| 9 | Invalid session key | Re-authenticate user |
| 11 | Service offline | Temporary, retry later |
| 16 | Temporarily unavailable | Rate limited, wait before retrying |
| 26 | Suspended account | User suspended |

### Error Handling Pattern
```csharp
try
{
    var response = await _apiClient.Scrobble(audio, lastfmUser).ConfigureAwait(false);
    
    if (response.IsError())
    {
        switch (response.Error)
        {
            case "3":
            case "9":
                _logger.LogWarning("Invalid session key, user needs to re-authenticate");
                // Mark user session as invalid, prompt re-login
                lastfmUser.SessionKey = null;
                break;
            
            case "7":
            case "16":
                _logger.LogWarning("API temporarily unavailable: {0}", response.Message);
                // Don't log as error, retry next track
                break;
            
            case "26":
                _logger.LogError("User account suspended: {0}", lastfmUser.Username);
                // Disable scrobbling for this user
                lastfmUser.Options.Scrobble = false;
                break;
            
            default:
                _logger.LogWarning("Last.fm error {0}: {1}", response.Error, response.Message);
                break;
        }
        return;
    }
}
catch (HttpRequestException ex)
{
    _logger.LogWarning(ex, "Network error calling Last.fm API");
    // Network error - don't propagate, continue playback
}
catch (JsonException ex)
{
    _logger.LogError(ex, "Malformed response from Last.fm");
    // JSON parsing error - likely API change, skip this request
}
```

---

## 8. RATE LIMITING

### Last.fm Rate Limit Policy
- **Free tier**: ~100 requests per IP per minute
- **Authenticated**: ~1000 requests per IP per minute
- **Recommendation**: Space requests 1 second apart for safety

### Implementation
```csharp
private DateTime _lastApiCallTime = DateTime.MinValue;
private const int MinMsecsBetweenCalls = 1000;

private async Task<TResponse> RateLimitedPost<TRequest, TResponse>(TRequest request)
    where TRequest : BaseRequest
    where TResponse : BaseResponse
{
    var elapsed = (DateTime.UtcNow - _lastApiCallTime).TotalMilliseconds;
    if (elapsed < MinMsecsBetweenCalls)
    {
        await Task.Delay((int)(MinMsecsBetweenCalls - elapsed)).ConfigureAwait(false);
    }
    
    _lastApiCallTime = DateTime.UtcNow;
    return await Post<TRequest, TResponse>(request);
}
```

---

## 9. TESTING WITH LAST.FM

### Integration Testing Checklist
- ✅ Invalid credentials → error 3
- ✅ Expired session → error 9 (need to re-auth)
- ✅ Scrobble with valid duration/playtime → success
- ✅ Scrobble with duration < 30s → rejected
- ✅ Scrobble with < 4min played → rejected
- ✅ Duplicate scrobble within 15s → success (but won't count)
- ✅ Network timeout → graceful failure
- ✅ Malformed response → error logged, continue
- ✅ Rate limit (error 16) → logged as warning, retry next track

### Test Last.fm Sandbox
```
Username: testuser@test.com
Password: Testpass123
```
(Note: Last.fm has no official sandbox. Use real account for testing.)

---

## 10. REQUEST/RESPONSE MODEL PATTERN

### BaseRequest.cs
```csharp
public abstract class BaseRequest
{
    public abstract string Method { get; }
    public string ApiKey { get; set; } = "your-api-key";
    
    public virtual Dictionary<string, string> ToDictionary()
    {
        var props = GetType().GetProperties();
        var dict = new Dictionary<string, string>();
        
        foreach (var prop in props)
        {
            var value = prop.GetValue(this);
            if (value == null) continue;
            
            var key = ConvertToLowerCamelCase(prop.Name);
            dict[key] = value.ToString();
        }
        
        return dict;
    }
}
```

### BaseResponse.cs
```csharp
public abstract class BaseResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
    
    public bool IsError() => !string.IsNullOrEmpty(Error);
}
```

---

## 11. SECURITY CONSIDERATIONS

### API Key Management
- ❌ Never commit API keys in code
- ✅ Store in environment variables or Jellyfin plugin config
- ✅ Use plugin's configuration system

### Session Key Security
- ❌ Never log session keys (even at debug level)
- ❌ Never transmit over unencrypted HTTP
- ✅ Always HTTPS for requests (though Last.fm uses HTTP)
- ✅ Store only in Jellyfin's encrypted config

### Password Handling
```csharp
// ✅ CORRECT: Exchange password for session, discard password
var session = await RequestSession(username, password);
lastfmUser.SessionKey = session.SessionKey;
// password is now out of scope, GC'd

// ❌ WRONG: Don't cache credentials
lastfmUser.Username = username;
lastfmUser.Password = password;  // NEVER
```

---

## 12. USEFUL RESOURCES

- **Official API Docs**: https://www.last.fm/api/
- **API Methods**: https://www.last.fm/api/show/
- **Test Methods**: https://www.last.fm/api/show/auth.getMobileSession
- **Error Codes**: https://www.last.fm/api/show/
- **Rate Limiting**: https://www.last.fm/api/#limits

---

**Last Updated**: January 24, 2026 | **Last.fm API**: 2.0
