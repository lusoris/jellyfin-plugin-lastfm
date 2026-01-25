# Jellyfin Core Interfaces

Essential service interfaces for plugin development.

## ISessionManager

Playback event handling.

**Namespace:** `MediaBrowser.Controller.Session`

```csharp
public interface ISessionManager
{
    // Playback Events
    event EventHandler<PlaybackProgressEventArgs> PlaybackStart;
    event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;
    event EventHandler<PlaybackStopEventArgs> PlaybackStopped;
    
    // Session Events
    event EventHandler<SessionEventArgs> SessionStarted;
    event EventHandler<SessionEventArgs> SessionEnded;
}
```

**Usage:**
```csharp
_sessionManager.PlaybackStart += OnPlaybackStart;
_sessionManager.PlaybackStopped += OnPlaybackStopped;
```

---

## IUserDataManager

User-specific data management.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public interface IUserDataManager
{
    event EventHandler<UserDataSaveEventArgs>? UserDataSaved;
    
    UserItemData? GetUserData(User user, BaseItem item);
    
    void SaveUserData(User user, BaseItem item, UserItemData userData, 
                      UserDataSaveReason reason, CancellationToken ct);
}
```

**Save Reasons:**
| Reason | Value | When |
|--------|-------|------|
| `PlaybackStart` | 1 | Playback begins |
| `PlaybackProgress` | 2 | Position update |
| `PlaybackFinished` | 3 | Playback ends |
| `TogglePlayed` | 4 | Mark played/unplayed |
| `UpdateUserRating` | 5 | Favorite/rating change |
| `Import` | 6 | External import |

---

## ILibraryManager

Library access and queries.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public interface ILibraryManager
{
    BaseItem? GetItemById(Guid id);
    IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery query);
    
    MusicArtist GetArtist(string name, DtoOptions options);
}
```

**Query Example:**
```csharp
var query = new InternalItemsQuery
{
    IncludeItemTypes = [BaseItemKind.Audio],
    IsFavorite = true,
    Recursive = true,
    Limit = 100
};
var tracks = _libraryManager.GetItemList(query).OfType<Audio>();
```

---

## IUserManager

User account access.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public interface IUserManager
{
    IEnumerable<User> Users { get; }
    User? GetUserById(Guid id);
    User? GetUserByName(string name);
}
```

---

**Related:** [api/jellyfin-playback.md](api/jellyfin-playback.md) | [api/jellyfin-userdata.md](api/jellyfin-userdata.md)
