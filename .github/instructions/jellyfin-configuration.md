# Configuration & Persistence

## Plugin Configuration (XML-based)

```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    [JsonPropertyName("lastfmUsers")]
    public LastfmUser[] LastfmUsers { get; set; }
}
```

**Storage**: Jellyfin stores this as XML in plugin config directory
**Persistence**: Auto-saves when modified
**Access**: `Plugin.Instance.PluginConfiguration`

---

## Last.fm User Model

```csharp
public class LastfmUser
{
    public string Username { get; set; }           // Last.fm username
    public string SessionKey { get; set; }         // Permanent session (API key)
    public Guid MediaBrowserUserId { get; set; }   // Links to Jellyfin user
    public LastFmUserOptions Options { get; set; }
}

public class LastFmUserOptions
{
    public bool Scrobble { get; set; }             // Enable scrobbling
    public bool SyncFavourites { get; set; }       // Enable loved tracks sync
    public bool AlternativeMode { get; set; }      // Scrobble on finish vs progress
}
```

---

## Configuration Access Patterns

### Read Configuration

```csharp
var config = Plugin.Instance.PluginConfiguration;

// Find user by Jellyfin ID
var lastfmUser = config.LastfmUsers
    .FirstOrDefault(u => u.MediaBrowserUserId == jellyfin_userId);

if (lastfmUser == null)
{
    _logger.LogWarning("No Last.fm config for user {0}", jellyfin_userId);
    return;
}

// Check if feature enabled
if (!lastfmUser.Options.Scrobble)
{
    return;  // User disabled scrobbling
}

// Get session key
var sessionKey = lastfmUser.SessionKey;
if (string.IsNullOrEmpty(sessionKey))
{
    _logger.LogWarning("User {0} not authenticated", lastfmUser.Username);
    return;
}
```

### Update Configuration

```csharp
var config = Plugin.Instance.PluginConfiguration;

// Add new user
var newUser = new LastfmUser
{
    Username = "lastfm_username",
    SessionKey = "permanent-session-key-from-api",
    MediaBrowserUserId = jellyfin_userId,
    Options = new LastFmUserOptions
    {
        Scrobble = true,
        SyncFavourites = true,
        AlternativeMode = false
    }
};

var usersList = config.LastfmUsers?.ToList() ?? new List<LastfmUser>();
usersList.Add(newUser);
config.LastfmUsers = usersList.ToArray();

// NOTE: Configuration auto-saves, no explicit save needed
```

### Modify Existing User

```csharp
var config = Plugin.Instance.PluginConfiguration;
var lastfmUser = config.LastfmUsers
    .FirstOrDefault(u => u.MediaBrowserUserId == userId);

if (lastfmUser != null)
{
    lastfmUser.Options.Scrobble = false;  // Disable scrobbling
    // Config auto-saves
}
```

---

## Special Flags

### Plugin.Syncing

Used to prevent real-time scrobbles during scheduled import:

```csharp
public class ImportLastfmData : IScheduledTask
{
    public async Task ExecuteAsync(IProgress<double> progress, 
                                    CancellationToken cancellationToken)
    {
        Plugin.Syncing = true;  // Suppress PlaybackStopped events
        try
        {
            // Import loved tracks...
        }
        finally
        {
            Plugin.Syncing = false;  // Re-enable scrobbling
        }
    }
}

// In ServerEntryPoint.PlaybackStopped:
if (Plugin.Syncing)
{
    return;  // Skip scrobble during import
}
```

---

## Security Notes

✅ **Do**:
- Store only SessionKey (never password)
- Clear/null SessionKey when authentication fails
- Access SessionKey via configuration

❌ **Don't**:
- Store passwords in config
- Log SessionKeys
- Transmit SessionKey in cleartext (always HTTPS)

---

**Related**:
- [jellyfin-architecture.md](jellyfin-architecture.md) - Plugin lifecycle
- [jellyfin-models.md](jellyfin-models.md) - Type reference
