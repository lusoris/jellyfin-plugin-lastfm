# Jellyfin Models & Types Reference

## Audio Item (MediaBrowser.Controller.Entities.Audio.Audio)

```csharp
var audio = item as Audio;

// Properties
audio.Name              // Track name (REQUIRED for scrobbling)
audio.Artists           // Artist names (IList<string>)
audio.Album             // Album name
audio.RunTimeTicks      // Duration (100-nanosecond ticks)
audio.Path              // File path
audio.ParentId          // Album/folder ID
audio.ProviderIds       // MusicBrainz IDs (MusicBrainzArtist, etc)

// Example
if (audio.RunTimeTicks.HasValue)
{
    var durationSeconds = audio.RunTimeTicks.Value / 10_000_000;
}
```

**Key Facts**:
- `RunTimeTicks` is in 100-nanosecond units (divide by 10,000,000 for seconds)
- `Artists` is a list, use `.FirstOrDefault()` for primary artist
- Always check `is not Audio` before casting

---

## User Data (MediaBrowser.Controller.Entities.UserItemData)

```csharp
var userData = e.UserData;

// Properties
userData.IsFavorite     // Is marked as favorite
userData.PlaybackTicks  // Seconds watched (for video)
userData.Rating         // User rating (0-10)
userData.Likes          // Like/Dislike state (bool?)
userData.PlayCount      // Number of times played
userData.LastPlayedDate // Last play timestamp
```

**Common Checks**:
```csharp
if (e.UserData.IsFavorite)
{
    // User marked as favorite
}

if (e.SaveReason == UserDataSaveReason.UpdateUserRating)
{
    // User changed rating/favorite status
}
```

---

## Jellyfin User (MediaBrowser.Controller.Entities.User)

```csharp
var user = e.Users?.FirstOrDefault();

// Properties
user.Username           // Display name
user.Id                 // Guid MediaBrowserUserId (IMPORTANT)
user.HasPassword        // Has password set
user.Policy             // User policy (admin status, etc)
```

**Key Pattern**:
```csharp
var users = _userManager.GetUsers()
    .Where(u => u.Id == userIdFromEvent)
    .FirstOrDefault();

// Link to Last.fm user via MediaBrowserUserId
var lastfmUser = config.LastfmUsers
    .FirstOrDefault(u => u.MediaBrowserUserId == user.Id);
```

---

## Playback Event Arguments

### PlaybackProgressEventArgs
```csharp
public class PlaybackProgressEventArgs
{
    public User[] Users { get; set; }           // Active users
    public BaseItem Item { get; set; }          // Item being played
    public long PlaybackPositionTicks { get; set; }  // Current position
    public SessionInfo SessionInfo { get; set; }
}
```

### PlaybackStopEventArgs
```csharp
public class PlaybackStopEventArgs
{
    public User[] Users { get; set; }
    public BaseItem Item { get; set; }
    public long PlaybackPositionTicks { get; set; }  // Total played
    public SessionInfo SessionInfo { get; set; }
}
```

### UserDataSaveEventArgs
```csharp
public class UserDataSaveEventArgs
{
    public Guid UserId { get; set; }
    public BaseItem Item { get; set; }
    public UserItemData UserData { get; set; }
    public UserDataSaveReason SaveReason { get; set; }
    
    public enum UserDataSaveReason
    {
        UpdateUserRating,       // Favorite/rating changed
        PlaybackFinished,       // Track finished playing
        PlaybackProgress,       // Progress tracking
        TogglePlayed,          // Played status toggled
        UpdateUserData         // Generic update
    }
}
```

---

## Item Type Checking

```csharp
// Audio (music)
if (item is not Audio audio) return;

// Video
if (item is Video video) { }

// Album/Series
if (item is MusicAlbum album) { }
if (item is Series series) { }

// Folder
if (item is Folder folder) { }

// Check base type
if (item is not BaseItem baseItem) { }
```

---

## Collections & LINQ Patterns

```csharp
// Get artists safely
var artists = audio.Artists ?? new List<string>();
var firstArtist = audio.Artists?.FirstOrDefault() ?? "Unknown";

// Filter users
var adminUsers = _userManager.GetUsers()
    .Where(u => u.Policy.IsAdministrator)
    .ToList();

// Find Last.fm user for Jellyfin user
var lastfmUser = config.LastfmUsers
    .FirstOrDefault(u => u.MediaBrowserUserId == jellyfin_UserId);

// Check if any user has scrobbling enabled
var hasScrobbling = config.LastfmUsers
    .Any(u => u.Options.Scrobble);
```

---

**Related**:
- [jellyfin-architecture.md](jellyfin-architecture.md) - Plugin lifecycle
- [jellyfin-configuration.md](jellyfin-configuration.md) - Config models
