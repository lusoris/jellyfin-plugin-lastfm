# Jellyfin User Data

User-specific data structures for tracking play history.

## UserItemData

**Namespace:** `MediaBrowser.Controller.Entities`

```csharp
public class UserItemData
{
    public string Key { get; set; }
    public int PlayCount { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime? LastPlayedDate { get; set; }
    public bool Played { get; set; }
    public long PlaybackPositionTicks { get; set; }
    public double? Rating { get; set; }  // 0.0 - 10.0
    public bool? Likes { get; set; }
}
```

## Reading User Data

```csharp
// Get user data for an item
var userData = _userDataManager.GetUserData(user, audio);

// Check favorite status
if (userData?.IsFavorite == true) { /* loved */ }

// Get play count
var playCount = userData?.PlayCount ?? 0;

// Get last played
var lastPlayed = userData?.LastPlayedDate;
```

## Writing User Data

```csharp
// Update favorite status
var userData = _userDataManager.GetUserData(user, item)
    ?? new UserItemData { Key = item.GetUserDataKeys().First() };

userData.IsFavorite = true;
_userDataManager.SaveUserData(
    user, 
    item, 
    userData, 
    UserDataSaveReason.UpdateUserRating,
    CancellationToken.None);
```

## Import External Data

```csharp
// Import play counts from Last.fm
var userData = _userDataManager.GetUserData(user, item) 
    ?? new UserItemData();

// Strategy: Use maximum of both values
userData.PlayCount = Math.Max(userData.PlayCount, lastfmPlayCount);
userData.LastPlayedDate = lastfmLastPlayed ?? userData.LastPlayedDate;
userData.Played = userData.PlayCount > 0;

_userDataManager.SaveUserData(
    user, 
    item, 
    userData, 
    UserDataSaveReason.Import,
    CancellationToken.None);
```

## UserDataSaved Event

Listen for user data changes:

```csharp
_userDataManager.UserDataSaved += OnUserDataSaved;

private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
{
    // Only care about favorite changes
    if (e.SaveReason != UserDataSaveReason.UpdateUserRating) return;
    if (e.Item is not Audio audio) return;
    
    var userData = _userDataManager.GetUserData(e.UserId, audio);
    
    if (userData?.IsFavorite == true)
    {
        // Sync to Last.fm: track.love
    }
    else
    {
        // Sync to Last.fm: track.unlove
    }
}
```

## User Data Keys

Jellyfin generates lookup keys for matching:

```csharp
// Audio keys (priority order):
// 1. "MusicBrainzTrack-{MusicBrainzRecording}"
// 2. "{AlbumArtist}-{Album}-{Name}" (normalized)

// Get keys for an item
var keys = item.GetUserDataKeys();
```

---

**Related:** [api/jellyfin-audio.md](api/jellyfin-audio.md) | [api/jellyfin-interfaces.md](api/jellyfin-interfaces.md)
