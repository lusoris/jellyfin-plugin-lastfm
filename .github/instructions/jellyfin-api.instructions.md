---
applyTo: "**/*.cs"
---

# Jellyfin API Reference for Last.fm Plugin

This document provides a comprehensive reference for Jellyfin APIs relevant to the Last.fm scrobbling plugin.

## Table of Contents

1. [Core Interfaces](#core-interfaces)
2. [User Data System](#user-data-system)
3. [Playback Events](#playback-events)
4. [Audio Entities](#audio-entities)
5. [Provider IDs & MusicBrainz](#provider-ids--musicbrainz)
6. [Library Manager](#library-manager)
7. [User Management](#user-management)
8. [Configuration System](#configuration-system)
9. [Dependency Injection](#dependency-injection)
10. [Sync Feature Matrix](#sync-feature-matrix)

---

## Core Interfaces

### ISessionManager

The central interface for playback event handling.

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
    event EventHandler<SessionEventArgs> SessionActivity;
    
    // Methods
    Task OnPlaybackStart(PlaybackStartInfo info);
    Task OnPlaybackProgress(PlaybackProgressInfo info, bool isAutomated);
    Task OnPlaybackStopped(PlaybackStopInfo info);
}
```

### IUserDataManager

Manages user-specific data for media items.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public interface IUserDataManager
{
    event EventHandler<UserDataSaveEventArgs>? UserDataSaved;
    
    void SaveUserData(User user, BaseItem item, UserItemData userData, 
                      UserDataSaveReason reason, CancellationToken cancellationToken);
    
    void SaveUserData(User user, BaseItem item, UpdateUserItemDataDto userDataDto, 
                      UserDataSaveReason reason);
    
    UserItemData? GetUserData(User user, BaseItem item);
    
    UserItemDataDto? GetUserDataDto(BaseItem item, User user);
    
    bool UpdatePlayState(BaseItem item, UserItemData data, long? reportedPositionTicks);
}
```

### ILibraryManager

Access to the media library.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public interface ILibraryManager
{
    BaseItem? GetItemById(Guid id);
    T? GetItemById<T>(Guid id) where T : BaseItem;
    T? GetItemById<T>(Guid id, User? user) where T : BaseItem;
    
    IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery query);
    QueryResult<BaseItem> GetItemsResult(InternalItemsQuery query);
    
    MusicArtist GetArtist(string name, DtoOptions options);
    MusicGenre GetMusicGenre(string name);
}
```

### IUserManager

User account management.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public interface IUserManager
{
    event EventHandler<GenericEventArgs<User>> OnUserUpdated;
    
    IEnumerable<User> Users { get; }
    IEnumerable<Guid> UsersIds { get; }
    
    User? GetUserById(Guid id);
    User? GetUserByName(string name);
}
```

---

## User Data System

### UserItemData

The core data structure for user-specific item data.

**Namespace:** `MediaBrowser.Controller.Entities`

```csharp
public class UserItemData
{
    /// <summary>
    /// Unique key for this user-item combination.
    /// </summary>
    public string Key { get; set; }
    
    /// <summary>
    /// User rating (0.0 - 10.0 scale).
    /// </summary>
    public double? Rating { get; set; }
    
    /// <summary>
    /// Current playback position in ticks (1 tick = 100 nanoseconds).
    /// </summary>
    public long PlaybackPositionTicks { get; set; }
    
    /// <summary>
    /// Total number of times this item has been played.
    /// </summary>
    public int PlayCount { get; set; }
    
    /// <summary>
    /// Whether this item is marked as a favorite.
    /// </summary>
    public bool IsFavorite { get; set; }
    
    /// <summary>
    /// The last time this item was played.
    /// </summary>
    public DateTime? LastPlayedDate { get; set; }
    
    /// <summary>
    /// Whether this item has been played/completed.
    /// </summary>
    public bool Played { get; set; }
    
    /// <summary>
    /// Whether the user likes this item (true/false/null).
    /// </summary>
    public bool? Likes { get; set; }
    
    /// <summary>
    /// Preferred audio stream index.
    /// </summary>
    public int? AudioStreamIndex { get; set; }
    
    /// <summary>
    /// Preferred subtitle stream index.
    /// </summary>
    public int? SubtitleStreamIndex { get; set; }
}
```

### UserItemDataDto

DTO for API responses.

**Namespace:** `MediaBrowser.Model.Dto`

```csharp
public class UserItemDataDto
{
    public double? Rating { get; set; }
    public double? PlayedPercentage { get; set; }
    public int? UnplayedItemCount { get; set; }
    public long PlaybackPositionTicks { get; set; }
    public int PlayCount { get; set; }
    public bool IsFavorite { get; set; }
    public bool? Likes { get; set; }
    public DateTime? LastPlayedDate { get; set; }
    public bool Played { get; set; }
    public required string Key { get; set; }
    public Guid ItemId { get; set; }
}
```

### UpdateUserItemDataDto

DTO for updating user data.

**Namespace:** `MediaBrowser.Model.Dto`

```csharp
public class UpdateUserItemDataDto
{
    public double? Rating { get; set; }
    public double? PlayedPercentage { get; set; }
    public int? UnplayedItemCount { get; set; }
    public long? PlaybackPositionTicks { get; set; }
    public int? PlayCount { get; set; }
    public bool? IsFavorite { get; set; }
    public bool? Likes { get; set; }
    public DateTime? LastPlayedDate { get; set; }
    public bool? Played { get; set; }
    public string? Key { get; set; }
    public Guid? ItemId { get; set; }
}
```

### UserDataSaveReason

Enum indicating why user data was saved.

**Namespace:** `MediaBrowser.Model.Entities`

```csharp
public enum UserDataSaveReason
{
    PlaybackStart = 1,
    PlaybackProgress = 2,
    PlaybackFinished = 3,
    TogglePlayed = 4,
    UpdateUserRating = 5,
    Import = 6,
    UpdateUserData = 7
}
```

---

## Playback Events

### PlaybackProgressEventArgs

Base class for playback event data.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public class PlaybackProgressEventArgs : EventArgs
{
    public List<User> Users { get; set; }
    public long? PlaybackPositionTicks { get; set; }
    public BaseItem Item { get; set; }
    public BaseItemDto MediaInfo { get; set; }
    public string MediaSourceId { get; set; }
    public bool IsPaused { get; set; }
    public bool IsAutomated { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string ClientName { get; set; }
    public string PlaySessionId { get; set; }
    public SessionInfo Session { get; set; }
}
```

### PlaybackStartEventArgs

Fired when playback begins.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public class PlaybackStartEventArgs : PlaybackProgressEventArgs
{
    // Inherits all properties from PlaybackProgressEventArgs
}
```

### PlaybackStopEventArgs

Fired when playback ends.

**Namespace:** `MediaBrowser.Controller.Library`

```csharp
public class PlaybackStopEventArgs : PlaybackProgressEventArgs
{
    /// <summary>
    /// Whether the item was played to completion.
    /// </summary>
    public bool PlayedToCompletion { get; set; }
}
```

### PlaybackProgressInfo

Progress information during playback.

**Namespace:** `MediaBrowser.Model.Session`

```csharp
public class PlaybackProgressInfo
{
    public bool CanSeek { get; set; }
    public BaseItemDto Item { get; set; }
    public Guid ItemId { get; set; }
    public string SessionId { get; set; }
    public string MediaSourceId { get; set; }
    public int? AudioStreamIndex { get; set; }
    public int? SubtitleStreamIndex { get; set; }
    public bool IsPaused { get; set; }
    public bool IsMuted { get; set; }
    public long? PositionTicks { get; set; }
    public long? PlaybackStartTimeTicks { get; set; }
    public int? VolumeLevel { get; set; }
    public int? Brightness { get; set; }
    public string AspectRatio { get; set; }
    public PlayMethod? PlayMethod { get; set; }
    public string LiveStreamId { get; set; }
    public string PlaySessionId { get; set; }
    public RepeatMode RepeatMode { get; set; }
    public PlaybackOrder PlaybackOrder { get; set; }
    public QueueItem[] NowPlayingQueue { get; set; }
    public string PlaylistItemId { get; set; }
}
```

### PlaybackStopInfo

Information when playback stops.

**Namespace:** `MediaBrowser.Model.Session`

```csharp
public class PlaybackStopInfo
{
    public BaseItemDto Item { get; set; }
    public Guid ItemId { get; set; }
    public string SessionId { get; set; }
    public string MediaSourceId { get; set; }
    public long? PositionTicks { get; set; }
    public string LiveStreamId { get; set; }
    public string PlaySessionId { get; set; }
    public bool Failed { get; set; }
    public string NextMediaType { get; set; }
    public string PlaylistItemId { get; set; }
}
```

---

## Audio Entities

### Audio (Track/Song)

Represents a single audio track.

**Namespace:** `MediaBrowser.Controller.Entities.Audio`

**BaseItemKind:** `BaseItemKind.Audio`

```csharp
public class Audio : BaseItem, IHasAlbumArtist, IHasArtist, IHasMusicGenres
{
    // From IHasAlbumArtist
    public IReadOnlyList<string> AlbumArtists { get; set; }
    
    // From IHasArtist
    public IReadOnlyList<string> Artists { get; set; }
    
    // Audio-specific
    public string Album { get; set; }
    
    // From BaseItem
    public string Name { get; }               // Track title
    public long? RunTimeTicks { get; }        // Duration in ticks
    public int? IndexNumber { get; }          // Track number
    public int? ParentIndexNumber { get; }    // Disc number
    
    // Provider IDs
    public Dictionary<string, string> ProviderIds { get; }
}
```

### MusicAlbum

Represents a music album.

**Namespace:** `MediaBrowser.Controller.Entities.Audio`

**BaseItemKind:** `BaseItemKind.MusicAlbum`

```csharp
public class MusicAlbum : Folder, IHasAlbumArtist, IHasArtist, IHasMusicGenres
{
    public IReadOnlyList<string> Artists { get; set; }
    public IReadOnlyList<string> AlbumArtists { get; set; }
    
    // Get all tracks in the album
    public IEnumerable<Audio> Tracks { get; }
    
    // Get the primary artist
    public MusicArtist GetMusicArtist(DtoOptions options);
    
    // Album artist name
    public string AlbumArtist { get; }
    
    // Provider IDs (MusicBrainz, AudioDB, etc.)
    public Dictionary<string, string> ProviderIds { get; }
}
```

### MusicArtist

Represents a music artist.

**Namespace:** `MediaBrowser.Controller.Entities.Audio`

**BaseItemKind:** `BaseItemKind.MusicArtist`

```csharp
public class MusicArtist : Folder, IItemByName, IHasMusicGenres
{
    public string Name { get; set; }
    public string Overview { get; set; }
    
    // Provider IDs
    public Dictionary<string, string> ProviderIds { get; }
}
```

### BaseItem Common Properties

All items inherit from BaseItem.

**Namespace:** `MediaBrowser.Controller.Entities`

```csharp
public abstract class BaseItem : IHasProviderIds
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string Overview { get; set; }
    public long? RunTimeTicks { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    
    // Provider IDs (MusicBrainz, etc.)
    public Dictionary<string, string> ProviderIds { get; set; }
    
    // User data helpers
    public virtual void MarkPlayed(User user, DateTime? datePlayed, bool resetPosition);
    public virtual void MarkUnplayed(User user);
    public virtual bool IsPlayed(User user, UserItemData userItemData);
    public virtual bool IsUnplayed(User user, UserItemData userItemData);
    public bool IsFavoriteOrLiked(User user, UserItemData userItemData);
    
    // User data key generation
    public virtual List<string> GetUserDataKeys();
}
```

---

## Provider IDs & MusicBrainz

### MetadataProvider Enum

Standard provider identifiers.

**Namespace:** `MediaBrowser.Model.Entities`

```csharp
public enum MetadataProvider
{
    Imdb = 2,
    Tmdb = 3,
    Tvdb = 4,
    MusicBrainzAlbum = 6,        // Release MBID
    MusicBrainzAlbumArtist = 9,  // Album artist MBID
    MusicBrainzArtist = 10,      // Artist MBID
    MusicBrainzReleaseGroup = 11,// Release group MBID
    AudioDbArtist = 16,
    AudioDbAlbum = 17,
    MusicBrainzTrack = 18,       // Track MBID
    MusicBrainzRecording = 19    // Recording MBID (unique to recording)
}
```

### IHasProviderIds Interface

Access provider IDs on items.

**Namespace:** `MediaBrowser.Model.Entities`

```csharp
public interface IHasProviderIds
{
    Dictionary<string, string> ProviderIds { get; set; }
}

// Extension methods
public static class ProviderIdsExtensions
{
    public static string? GetProviderId(this IHasProviderIds instance, MetadataProvider provider);
    public static string? GetProviderId(this IHasProviderIds instance, string name);
    public static bool TryGetProviderId(this IHasProviderIds instance, MetadataProvider provider, out string? id);
    public static void SetProviderId(this IHasProviderIds instance, MetadataProvider provider, string? value);
    public static void SetProviderId(this IHasProviderIds instance, string name, string? value);
    public static bool TrySetProviderId(this IHasProviderIds instance, MetadataProvider provider, string? value);
}
```

### MusicBrainz ID Mapping

| Audio File Tag | Jellyfin Provider | Description |
|----------------|-------------------|-------------|
| MUSICBRAINZ_TRACKID | MusicBrainzRecording | Recording MBID (unique per recording) |
| MUSICBRAINZ_RELEASETRACKID | MusicBrainzTrack | Track MBID (unique per release) |
| MUSICBRAINZ_ALBUMID | MusicBrainzAlbum | Release MBID |
| MUSICBRAINZ_RELEASEGROUPID | MusicBrainzReleaseGroup | Release Group MBID |
| MUSICBRAINZ_ARTISTID | MusicBrainzArtist | Artist MBID |
| MUSICBRAINZ_ALBUMARTISTID | MusicBrainzAlbumArtist | Album Artist MBID |

### User Data Key Generation

Jellyfin generates user data keys for lookup.

```csharp
// For Audio items, keys are generated in this priority:
// 1. "MusicBrainzTrack-{MusicBrainzRecording}"
// 2. "{AlbumArtist}-{Album}-{Name}" (normalized)

// For MusicAlbum:
// 1. "MusicAlbum-Musicbrainz-{MusicBrainzAlbum}"
// 2. "MusicAlbum-MusicBrainzReleaseGroup-{MusicBrainzReleaseGroup}"
// 3. "{AlbumArtist}-{Name}"

// For MusicArtist:
// 1. "Artist-Musicbrainz-{MusicBrainzArtist}"
// 2. "Artist-{Name}" (normalized, diacritics removed)
```

---

## Library Manager

### InternalItemsQuery

Query object for searching the library.

**Namespace:** `MediaBrowser.Controller.Entities`

```csharp
public class InternalItemsQuery
{
    public User? User { get; set; }
    public Guid[] ItemIds { get; set; }
    public Guid ParentId { get; set; }
    public Guid[] AncestorIds { get; set; }
    
    public BaseItemKind[] IncludeItemTypes { get; set; }
    public BaseItemKind[] ExcludeItemTypes { get; set; }
    
    public string? Name { get; set; }
    public string? NameContains { get; set; }
    public string? NameStartsWith { get; set; }
    public string? Path { get; set; }
    
    public bool? IsFavorite { get; set; }
    public bool? IsPlayed { get; set; }
    
    public string[] Artists { get; set; }
    public string[] AlbumArtistNames { get; set; }
    public string[] Albums { get; set; }
    public Guid[] GenreIds { get; set; }
    
    public bool Recursive { get; set; }
    public int? Limit { get; set; }
    public int? StartIndex { get; set; }
    
    public (ItemSortBy, SortOrder)[] OrderBy { get; set; }
    
    public DtoOptions DtoOptions { get; set; }
    
    public bool EnableTotalRecordCount { get; set; }
}
```

### BaseItemKind Enum (Audio-related)

**Namespace:** `Jellyfin.Data.Enums`

```csharp
public enum BaseItemKind
{
    Audio,           // Individual song/track
    AudioBook,       // Audiobook
    MusicAlbum,      // Album
    MusicArtist,     // Artist
    MusicGenre,      // Genre
    MusicVideo,      // Music video
    Playlist,        // Playlist
    // ... other item types
}
```

### ItemSortBy Enum (Audio-related)

**Namespace:** `Jellyfin.Data.Enums`

```csharp
public enum ItemSortBy
{
    Album,
    AlbumArtist,
    Artist,
    DateCreated,
    DatePlayed,        // Last played date
    PlayCount,         // Number of plays
    Name,
    SortName,
    Random,
    Runtime,
    // ... other sort options
}
```

---

## User Management

### User Entity

**Namespace:** `Jellyfin.Database.Implementations.Entities`

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string? Password { get; set; }
    
    // Preferences
    public bool EnableAutoLogin { get; set; }
    public bool HidePlayedInLatest { get; set; }
    public bool RememberAudioSelections { get; set; }
    public bool RememberSubtitleSelections { get; set; }
    
    // Permissions
    public bool IsAdministrator { get; set; }
    public bool IsDisabled { get; set; }
}
```

---

## Configuration System

### Plugin Configuration Base

**Namespace:** `MediaBrowser.Model.Plugins`

```csharp
public class BasePluginConfiguration
{
    // Base class for plugin settings
    // Serialized to XML in plugin config directory
}
```

### Server Configuration

**Namespace:** `MediaBrowser.Model.Configuration`

Relevant playback settings:

```csharp
public class ServerConfiguration
{
    // Minimum percentage of playback before considering it "played"
    public int MinResumePct { get; set; }           // Default: 5
    
    // Maximum percentage remaining to auto-mark as complete
    public int MaxResumePct { get; set; }           // Default: 90
    
    // Minutes before resume position is ignored (audiobooks)
    public int MinAudiobookResume { get; set; }     // Default: 5
    
    // Minutes remaining to auto-complete (audiobooks)
    public int MaxAudiobookResume { get; set; }     // Default: 5
}
```

---

## Dependency Injection

### Service Registration

**Namespace:** `MediaBrowser.Controller`

```csharp
public interface IServerEntryPoint : IDisposable
{
    // Called when the server starts
    Task RunAsync();
}

// Register in PluginServiceRegistrator
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register your services here
        serviceCollection.AddHostedService<MyBackgroundService>();
    }
}
```

### Available Services via DI

```csharp
// Commonly injected services:
ISessionManager          // Playback events
IUserDataManager         // User data (play counts, favorites, etc.)
ILibraryManager          // Library access
IUserManager             // User management
ILogger<T>               // Logging
IServerConfigurationManager  // Server configuration
IApplicationPaths        // File system paths
IHttpClientFactory       // HTTP requests
```

---

## Sync Feature Matrix

### Jellyfin → Last.fm (Outbound)

| Jellyfin Event | Last.fm API | Data Sent |
|----------------|-------------|-----------|
| `PlaybackStart` | `track.updateNowPlaying` | artist, track, album, duration |
| `PlaybackStopped` (>50% played) | `track.scrobble` | artist, track, album, timestamp, duration |
| `UserData.IsFavorite = true` | `track.love` | artist, track |
| `UserData.IsFavorite = false` | `track.unlove` | artist, track |

### Last.fm → Jellyfin (Inbound)

| Last.fm API | Jellyfin Field | Notes |
|-------------|----------------|-------|
| `user.getLovedTracks` | `UserItemData.IsFavorite` | Sync loved tracks |
| `user.getTopTracks` | `UserItemData.PlayCount` | **New!** Sync play counts |
| `user.getRecentTracks` | `UserItemData.LastPlayedDate` | Sync last played dates |

### Matching Strategy

```csharp
// Priority for matching tracks between Jellyfin and Last.fm:
// 1. MusicBrainz Recording ID (exact match)
// 2. MusicBrainz Track ID (exact match)
// 3. Artist + Track Name (fuzzy match)
// 4. Artist + Album + Track Name (fuzzy match)

// For albums:
// 1. MusicBrainz Release Group ID
// 2. MusicBrainz Release ID
// 3. Artist + Album Name (fuzzy match)
```

---

## Example: Subscribing to Playback Events

```csharp
public class LastfmScrobbler : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<LastfmScrobbler> _logger;

    public LastfmScrobbler(
        ISessionManager sessionManager,
        IUserDataManager userDataManager,
        ILogger<LastfmScrobbler> logger)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;
        _logger = logger;
    }

    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _userDataManager.UserDataSaved += OnUserDataSaved;
        
        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Item is not Audio audio) return;
        
        var artists = audio.Artists;
        var track = audio.Name;
        var album = audio.Album;
        var duration = audio.RunTimeTicks.HasValue 
            ? TimeSpan.FromTicks(audio.RunTimeTicks.Value)
            : (TimeSpan?)null;
        
        // Get MusicBrainz IDs if available
        audio.TryGetProviderId(MetadataProvider.MusicBrainzRecording, out var mbid);
        
        // Send Now Playing to Last.fm...
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Item is not Audio audio) return;
        if (!e.PlayedToCompletion) return;
        
        // Scrobble to Last.fm...
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        if (e.SaveReason != UserDataSaveReason.UpdateUserRating) return;
        if (e.Item is not Audio audio) return;
        
        var userData = _userDataManager.GetUserData(e.UserId, audio);
        
        // Sync favorite status to Last.fm...
        if (userData?.IsFavorite == true)
        {
            // track.love
        }
        else
        {
            // track.unlove
        }
    }

    public void Dispose()
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _userDataManager.UserDataSaved -= OnUserDataSaved;
    }
}
```

---

## Example: Querying Music Library

```csharp
public async Task<IEnumerable<Audio>> GetUserFavoriteTracks(User user)
{
    var query = new InternalItemsQuery(user)
    {
        IncludeItemTypes = new[] { BaseItemKind.Audio },
        IsFavorite = true,
        Recursive = true,
        OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
        Limit = 100
    };

    return _libraryManager.GetItemList(query).OfType<Audio>();
}

public async Task<IEnumerable<Audio>> GetTracksByMusicBrainzId(string mbid)
{
    // Search by provider ID
    var query = new InternalItemsQuery
    {
        IncludeItemTypes = new[] { BaseItemKind.Audio },
        Recursive = true
    };

    return _libraryManager.GetItemList(query)
        .OfType<Audio>()
        .Where(a => a.GetProviderId(MetadataProvider.MusicBrainzRecording) == mbid);
}
```

---

## Example: Updating User Data

```csharp
public async Task SyncPlayCountFromLastFm(User user, Audio track, int lastfmPlayCount)
{
    var userData = _userDataManager.GetUserData(user, track);
    if (userData == null) return;
    
    // Only update if Last.fm has more plays
    if (lastfmPlayCount > userData.PlayCount)
    {
        var updateDto = new UpdateUserItemDataDto
        {
            PlayCount = lastfmPlayCount
        };
        
        _userDataManager.SaveUserData(user, track, updateDto, UserDataSaveReason.Import);
    }
}

public async Task SyncLovedTrackFromLastFm(User user, Audio track, bool isLoved)
{
    var userData = _userDataManager.GetUserData(user, track);
    if (userData == null) return;
    
    if (userData.IsFavorite != isLoved)
    {
        var updateDto = new UpdateUserItemDataDto
        {
            IsFavorite = isLoved
        };
        
        _userDataManager.SaveUserData(user, track, updateDto, UserDataSaveReason.Import);
    }
}
```

---

## Important Notes

### Time Units
- **Ticks:** 1 tick = 100 nanoseconds = 0.0001 milliseconds
- **Conversion:** `TimeSpan.FromTicks(ticks)` or `ticks / TimeSpan.TicksPerSecond`

### Scrobble Threshold
Jellyfin considers an item "played to completion" based on:
- `MinResumePct` (default 5%): Ignore progress below this
- `MaxResumePct` (default 90%): Auto-complete above this

For Last.fm compliance, scrobble when:
- Track duration > 30 seconds AND
- Playback position > 50% OR > 4 minutes

### Thread Safety
- Session events can fire from multiple threads
- Use `ConcurrentDictionary` for caches
- Be careful with async void event handlers

### Disposal
- Always unsubscribe from events in `Dispose()`
- Implement `IAsyncDisposable` for async cleanup
