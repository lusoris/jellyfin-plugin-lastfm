---
applyTo: "**/*.cs"
---

# Jellyfin API Index

Quick reference for Jellyfin plugin development.

## ⚠️ EFCore Note (Jellyfin 10.11+)

Jellyfin migrated to EFCore internally, but **plugins are unaffected**. Use Jellyfin service interfaces—never access the database directly.

## Modules

| Topic | Document | Key Types |
|-------|----------|-----------|
| **Core Interfaces** | → [api/jellyfin-interfaces.md](api/jellyfin-interfaces.md) | `ISessionManager`, `IUserDataManager`, `ILibraryManager` |
| **Playback Events** | → [api/jellyfin-playback.md](api/jellyfin-playback.md) | `PlaybackStopEventArgs`, scrobble logic |
| **Audio Entities** | → [api/jellyfin-audio.md](api/jellyfin-audio.md) | `Audio`, `MusicAlbum`, `MusicArtist`, MBIDs |
| **User Data** | → [api/jellyfin-userdata.md](api/jellyfin-userdata.md) | `UserItemData`, favorites, play counts |
| **Last.fm API** | → [lastfm-api.instructions.md](lastfm-api.instructions.md) | Authentication, scrobbling, imports |
| **Cross-Reference** | → [api-cross-reference.instructions.md](api-cross-reference.instructions.md) | Jellyfin ↔ Last.fm mapping |

## Quick Reference

### Scrobble Flow
```
PlaybackStopped → Check eligibility → Build Scrobble → API call
```

### Favorite Sync
```
UserDataSaved (IsFavorite) → track.love / track.unlove
```

### Import Flow
```
user.getLovedTracks → Match in library → Update UserItemData
```

## Common Patterns

```csharp
// Get Audio from event
if (e.Item is not Audio audio) return;

// Get MusicBrainz ID
audio.TryGetProviderId(MetadataProvider.MusicBrainzRecording, out var mbid);

// Query library
var query = new InternalItemsQuery { 
    IncludeItemTypes = [BaseItemKind.Audio],
    Recursive = true 
};
var tracks = _libraryManager.GetItemList(query).OfType<Audio>();

// Get/update user data
var userData = _userDataManager.GetUserData(user, item);
```

---

**Related:** [csharp/csharp-patterns.md](csharp/csharp-patterns.md) | [workflow/testing.instructions.md](workflow/testing.instructions.md)
