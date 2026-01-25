---
applyTo: "**/*.cs"
---

# Last.fm ↔ Jellyfin API Cross-Reference

This document maps Last.fm API endpoints to Jellyfin APIs for implementing all plugin features.

## Table of Contents

1. [Core Sync Operations](#core-sync-operations)
2. [User Data Synchronization](#user-data-synchronization)
3. [Playlist Generation Strategies](#playlist-generation-strategies)
4. [Homepage & UI Integration](#homepage--ui-integration)
5. [Metadata Enhancement](#metadata-enhancement)
6. [Smart Matching Algorithms](#smart-matching-algorithms)
7. [Background Tasks](#background-tasks)
8. [Feature Implementation Matrix](#feature-implementation-matrix)

---

## Core Sync Operations

### 1. Scrobbling (Jellyfin → Last.fm)

**Trigger:** `ISessionManager.PlaybackStopped` event

| Jellyfin Source | Last.fm Target | Notes |
|-----------------|----------------|-------|
| `Audio.Artists[0]` | `track.scrobble.artist` | Primary artist |
| `Audio.Name` | `track.scrobble.track` | Track name |
| `Audio.Album` | `track.scrobble.album` | Optional |
| `Audio.AlbumArtists[0]` | `track.scrobble.albumArtist` | If different |
| `Audio.RunTimeTicks` | `track.scrobble.duration` | Convert ticks to seconds |
| `PlaybackStopEventArgs.PlaybackPositionTicks` | Calculate % | Scrobble if >50% or >4min |
| `Audio.ProviderIds["MusicBrainzRecording"]` | `track.scrobble.mbid` | Better matching |

**Implementation Flow:**
```
PlaybackStopped event
  → Check if Audio type
  → Check scrobble threshold (>50% or >4min)
  → Check minimum duration (>30sec)
  → Extract track metadata
  → Call track.scrobble API
  → Handle response/errors
```

### 2. Now Playing (Jellyfin → Last.fm)

**Trigger:** `ISessionManager.PlaybackStart` event

| Jellyfin Source | Last.fm Target |
|-----------------|----------------|
| `Audio.Artists[0]` | `track.updateNowPlaying.artist` |
| `Audio.Name` | `track.updateNowPlaying.track` |
| `Audio.Album` | `track.updateNowPlaying.album` |
| `Audio.RunTimeTicks` | `track.updateNowPlaying.duration` |

### 3. Love/Unlove Sync (Bidirectional)

**Jellyfin → Last.fm:**
- Trigger: `IUserDataManager.UserDataSaved` with `IsFavorite` change
- API: `track.love` or `track.unlove`

**Last.fm → Jellyfin:**
- Trigger: Scheduled task or manual sync
- API: `user.getLovedTracks`
- Target: `UserItemData.IsFavorite`

| Last.fm Response | Jellyfin Target |
|------------------|-----------------|
| `lovedtracks.track[].name` | Match via `ILibraryManager.GetItemList()` |
| `lovedtracks.track[].artist.name` | Artist filter |
| `lovedtracks.track[].date.uts` | `UserItemData.LastPlayedDate` (optional) |

---

## User Data Synchronization

### Play Count Import (Last.fm → Jellyfin)

**API Chain:**
```
user.getTopTracks(period=overall) 
  → foreach track
    → Match in Jellyfin library
    → Compare UserItemData.PlayCount
    → Update if configured (add/replace/max strategy)
```

| Last.fm Field | Jellyfin Target | Strategy Options |
|---------------|-----------------|------------------|
| `toptracks.track[].playcount` | `UserItemData.PlayCount` | Add, Replace, Max |

**Matching Priority:**
1. MusicBrainz Recording ID (exact)
2. Artist + Track name (fuzzy, normalized)
3. Artist + Album + Track (fuzzy, for disambiguation)

### Last Played Date Import (Last.fm → Jellyfin)

**API Chain:**
```
user.getRecentTracks(extended=1)
  → foreach track with date
    → Match in Jellyfin library
    → Update UserItemData.LastPlayedDate
```

| Last.fm Field | Jellyfin Target |
|---------------|-----------------|
| `recenttracks.track[].date.uts` | `UserItemData.LastPlayedDate` |

---

## Playlist Generation Strategies

### Strategy 1: Similar Artists Playlist

**API Flow:**
```
user.getTopArtists(limit=10) OR user.getLovedTracks → extract artists
  → foreach artist
    → artist.getSimilar(limit=5)
    → foreach similar artist
      → ILibraryManager.GetItemList(Artists=[similar])
      → Add to playlist candidates
  → Filter duplicates, limit results
  → IPlaylistManager.CreatePlaylist()
```

| Step | API | Data Flow |
|------|-----|-----------|
| 1. Get seed artists | `user.getTopArtists` | User's top artists |
| 2. Get similar | `artist.getSimilar` | Similar artist names |
| 3. Find in library | `ILibraryManager.GetItemList` | Local tracks |
| 4. Create playlist | `IPlaylistManager.CreatePlaylist` | New playlist |

### Strategy 2: Similar Tracks Playlist

**API Flow:**
```
user.getRecentTracks(limit=20) OR user.getLovedTracks
  → foreach track
    → track.getSimilar(limit=10)
    → Match similar tracks in local library
  → Deduplicate, sort by match score
  → IPlaylistManager.CreatePlaylist()
```

### Strategy 3: Rediscover Favorites

**API Flow:**
```
user.getLovedTracks(limit=200)
  → Match all in Jellyfin library
  → IUserDataManager.GetUserData() for each
  → Filter by LastPlayedDate > 30 days ago
  → Sort by age (oldest first)
  → IPlaylistManager.CreatePlaylist()
```

### Strategy 4: Weekly Mixtape

**API Flow:**
```
user.getWeeklyTrackChart()
  → Match in library
  → user.getTopArtists(period=7day)
    → artist.getSimilar for each
    → Find tracks in library
  → Combine: 50% weekly chart + 50% recommendations
  → IPlaylistManager.CreatePlaylist()
```

### Strategy 5: Tag/Genre Discovery

**API Flow:**
```
user.getTopTags(limit=10)
  → foreach tag
    → tag.getTopTracks(limit=20)
    → Match in Jellyfin library (by artist+track or MBID)
  → Filter to unplayed or rarely played
  → IPlaylistManager.CreatePlaylist()
```

---

## Homepage & UI Integration

### Option 1: Custom Plugin Pages (Main Menu)

Using `IHasWebPages` and `PluginPageInfo`:

```csharp
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public IEnumerable<PluginPageInfo> GetPages()
    {
        // Page in Music section of main menu
        yield return new PluginPageInfo
        {
            Name = "LastfmRecommendations",
            DisplayName = "Last.fm For You",
            EmbeddedResourcePath = GetType().Namespace + ".Pages.recommendations.html",
            EnableInMainMenu = true,
            MenuSection = "music",  // Appears under Music!
            MenuIcon = "stars"      // Material icon name
        };
        
        // Statistics page
        yield return new PluginPageInfo
        {
            Name = "LastfmStats",
            DisplayName = "Listening Stats",
            EmbeddedResourcePath = GetType().Namespace + ".Pages.statistics.html",
            EnableInMainMenu = true,
            MenuSection = "music",
            MenuIcon = "insights"
        };
        
        // Configuration (in admin area)
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
        };
    }
}
```

**Page URL:** `/web/configurationpage?name=LastfmRecommendations`

### Option 2: Custom REST API Endpoints

Create API endpoints that the custom pages can call:

```csharp
[ApiController]
[Authorize]
[Route("Lastfm")]
[Produces(MediaTypeNames.Application.Json)]
public class LastfmController : ControllerBase
{
    // Get personalized recommendations for homepage widget
    [HttpGet("Recommendations/{userId}")]
    public async Task<ActionResult<LastfmRecommendationsDto>> GetRecommendations(
        [FromRoute] Guid userId,
        [FromQuery] int limit = 20)
    {
        // Combine multiple recommendation sources
        var similarArtists = await GetSimilarArtistTracks(userId, 10);
        var similarTracks = await GetSimilarTracks(userId, 10);
        
        return new LastfmRecommendationsDto
        {
            SimilarArtists = similarArtists,
            SimilarTracks = similarTracks,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    // Get user's Last.fm stats
    [HttpGet("Stats/{userId}")]
    public async Task<ActionResult<LastfmStatsDto>> GetUserStats([FromRoute] Guid userId)
    {
        var config = GetUserConfig(userId);
        var lastfmUser = await _apiClient.GetUserInfo(config.Username);
        var topArtists = await _apiClient.GetTopArtists(config.Username, "7day", 5);
        var topTracks = await _apiClient.GetTopTracks(config.Username, "7day", 5);
        
        return new LastfmStatsDto
        {
            TotalScrobbles = lastfmUser.PlayCount,
            TopArtistsThisWeek = topArtists,
            TopTracksThisWeek = topTracks
        };
    }
    
    // Generate playlist on-demand
    [HttpPost("Playlists/Generate")]
    public async Task<ActionResult<PlaylistCreationResult>> GeneratePlaylist(
        [FromQuery] Guid userId,
        [FromQuery] string strategy,
        [FromQuery] int limit = 50)
    {
        var tracks = strategy switch
        {
            "similar-artists" => await GetSimilarArtistTracks(userId, limit),
            "similar-tracks" => await GetSimilarTracks(userId, limit),
            "rediscover" => await GetRediscoverTracks(userId, limit),
            "weekly" => await GetWeeklyMixtape(userId, limit),
            _ => throw new ArgumentException("Unknown strategy")
        };
        
        var request = new PlaylistCreationRequest
        {
            Name = $"Last.fm {strategy} - {DateTime.Now:MMM dd}",
            ItemIdList = tracks.Select(t => t.Id).ToList(),
            UserId = userId,
            MediaType = MediaType.Audio
        };
        
        return await _playlistManager.CreatePlaylist(request);
    }
}
```

### Option 3: HomeSection Integration (Advanced)

Jellyfin's home screen uses `HomeSectionType` enum. While plugins can't add new section types directly, you can:

1. **Create a custom widget via JavaScript** in your plugin page
2. **Use the "Resume" patterns** - create playlists that appear in "Continue Listening"
3. **Leverage the Suggestions API** - add tracks to user's play history to influence suggestions

**HomeSectionType values (for reference):**
```csharp
public enum HomeSectionType
{
    None = 0,
    SmallLibraryTiles = 1,    // Library folders
    LibraryButtons = 2,        // Library quick access
    ActiveRecordings = 3,      // Live TV
    Resume = 4,                // Continue Watching
    ResumeAudio = 5,           // Continue Listening
    LatestMedia = 6,           // Recently Added
    NextUp = 7,                // Next Episode
    LiveTv = 8                 // Live TV Now
}
```

### Homepage Widget Ideas

#### 1. "Last.fm For You" Section
Shows personalized recommendations based on Last.fm data:
- Similar artists you don't have
- Tracks similar to your loved songs
- "You might like" based on listening history

#### 2. "Listening Stats" Widget
Quick stats overlay:
- Scrobbles this week
- Top artist of the week
- Listening streak

#### 3. "Rediscover" Section
Surfaces old favorites:
- "Haven't heard in a while" 
- Based on `user.getLovedTracks` + local play history

#### 4. "Weekly Mixtape" Auto-Playlist
Auto-generated playlist that updates weekly:
- Mix of Last.fm recommendations + local favorites
- Shown prominently in Music section

---

## Metadata Enhancement

### Artist Images (IRemoteImageProvider)

**API Flow:**
```
artist.getInfo(artist=name)
  → response.artist.image
  → Return ImageInfo array
```

| Last.fm Field | Jellyfin Target |
|---------------|-----------------|
| `artist.image[size=extralarge]` | `RemoteImageInfo.Url` |
| `artist.image[size=mega]` | Primary image |

### Album Images (IRemoteImageProvider)

**API Flow:**
```
album.getInfo(artist=name, album=name)
  → response.album.image
  → Return ImageInfo array
```

### Track/Artist Bio

**API Flow:**
```
artist.getInfo(artist=name)
  → response.artist.bio.content
  → Set MusicArtist.Overview
```

---

## Smart Matching Algorithms

### Track Matching Priority

```csharp
public async Task<Audio?> MatchTrack(LastfmTrack lfmTrack)
{
    // 1. Try MusicBrainz Recording ID (exact match)
    if (!string.IsNullOrEmpty(lfmTrack.Mbid))
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true
        };
        
        var byMbid = _libraryManager.GetItemList(query)
            .OfType<Audio>()
            .FirstOrDefault(a => 
                a.GetProviderId(MetadataProvider.MusicBrainzRecording) == lfmTrack.Mbid);
        
        if (byMbid != null) return byMbid;
    }
    
    // 2. Exact artist + track name (case-insensitive)
    var byExact = _libraryManager.GetItemList(new InternalItemsQuery
    {
        IncludeItemTypes = [BaseItemKind.Audio],
        Artists = [lfmTrack.Artist],
        Name = lfmTrack.Name,
        Recursive = true
    }).OfType<Audio>().FirstOrDefault();
    
    if (byExact != null) return byExact;
    
    // 3. Fuzzy match (normalized names)
    var normalizedArtist = Normalize(lfmTrack.Artist);
    var normalizedTrack = Normalize(lfmTrack.Name);
    
    var allTracks = _libraryManager.GetItemList(new InternalItemsQuery
    {
        IncludeItemTypes = [BaseItemKind.Audio],
        Recursive = true
    }).OfType<Audio>();
    
    return allTracks.FirstOrDefault(a => 
        Normalize(a.Artists.FirstOrDefault()) == normalizedArtist &&
        Normalize(a.Name) == normalizedTrack);
}

private string Normalize(string? input)
{
    if (string.IsNullOrEmpty(input)) return string.Empty;
    
    return input
        .ToLowerInvariant()
        .Replace("'", "")
        .Replace("'", "")
        .Replace("\"", "")
        .Replace("(", "")
        .Replace(")", "")
        .Replace("-", " ")
        .Replace("  ", " ")
        .Trim();
}
```

### Artist Matching

```csharp
public MusicArtist? MatchArtist(string lastfmArtist, string? mbid = null)
{
    // 1. MusicBrainz Artist ID
    if (!string.IsNullOrEmpty(mbid))
    {
        var byMbid = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.MusicArtist],
            Recursive = true
        }).OfType<MusicArtist>()
         .FirstOrDefault(a => 
            a.GetProviderId(MetadataProvider.MusicBrainzArtist) == mbid);
        
        if (byMbid != null) return byMbid;
    }
    
    // 2. Exact name match
    try
    {
        return _libraryManager.GetArtist(lastfmArtist, new DtoOptions());
    }
    catch
    {
        // Artist not found
    }
    
    // 3. Fuzzy match (handle "The" prefix, special chars)
    var normalized = NormalizeArtist(lastfmArtist);
    var allArtists = _libraryManager.GetItemList(new InternalItemsQuery
    {
        IncludeItemTypes = [BaseItemKind.MusicArtist],
        Recursive = true
    }).OfType<MusicArtist>();
    
    return allArtists.FirstOrDefault(a => 
        NormalizeArtist(a.Name) == normalized);
}

private string NormalizeArtist(string name)
{
    var n = Normalize(name);
    if (n.StartsWith("the ")) n = n.Substring(4);
    return n;
}
```

---

## Background Tasks

### Scheduled Sync Task

```csharp
public class LastfmSyncTask : IScheduledTask
{
    public string Name => "Last.fm Sync";
    public string Category => "Last.fm";
    
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(6).Ticks
            }
        ];
    }
    
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken token)
    {
        var users = _userManager.Users
            .Where(u => HasLastfmConfig(u))
            .ToList();
        
        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var config = GetUserConfig(user);
            
            // Import loved tracks
            if (config.ImportLovedTracks)
            {
                await SyncLovedTracks(user, token);
            }
            
            // Import play counts
            if (config.ImportPlayCounts)
            {
                await SyncPlayCounts(user, token);
            }
            
            // Generate playlists
            if (config.AutoGeneratePlaylists)
            {
                await GeneratePlaylists(user, token);
            }
            
            progress.Report((i + 1) * 100.0 / users.Count);
        }
    }
}
```

### Playlist Auto-Generation Task

```csharp
public class LastfmPlaylistTask : IScheduledTask
{
    public string Name => "Last.fm Playlist Generation";
    
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Monday,
                TimeOfDayTicks = TimeSpan.FromHours(6).Ticks
            }
        ];
    }
    
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken token)
    {
        // Generate Weekly Mixtape for each user
        foreach (var user in GetConfiguredUsers())
        {
            var config = GetUserConfig(user);
            
            if (config.WeeklyMixtape.Enabled)
            {
                await GenerateWeeklyMixtape(user, config.WeeklyMixtape, token);
            }
        }
    }
}
```

---

## Feature Implementation Matrix

### Complete API Mapping

| Feature | Last.fm API(s) | Jellyfin API(s) | Direction | Priority |
|---------|---------------|-----------------|-----------|----------|
| **Core** |
| Scrobble | `track.scrobble` | `ISessionManager.PlaybackStopped` | JF→LFM | P0 |
| Now Playing | `track.updateNowPlaying` | `ISessionManager.PlaybackStart` | JF→LFM | P0 |
| Auth | `auth.getMobileSession` | Plugin config | Setup | P0 |
| **Favorites Sync** |
| Love to LFM | `track.love` | `IUserDataManager.UserDataSaved` | JF→LFM | P1 |
| Love from LFM | `user.getLovedTracks` | `IUserDataManager.SaveUserData` | LFM→JF | P1 |
| **Data Import** |
| Play Count | `user.getTopTracks` | `UserItemData.PlayCount` | LFM→JF | P1 |
| Last Played | `user.getRecentTracks` | `UserItemData.LastPlayedDate` | LFM→JF | P2 |
| **Playlists** |
| Similar Artists | `user.getTopArtists` + `artist.getSimilar` | `IPlaylistManager.CreatePlaylist` | LFM→JF | P2 |
| Similar Tracks | `user.getRecentTracks` + `track.getSimilar` | `IPlaylistManager.CreatePlaylist` | LFM→JF | P2 |
| Rediscover | `user.getLovedTracks` + local filter | `IPlaylistManager.CreatePlaylist` | LFM→JF | P2 |
| Weekly Mixtape | `user.getWeeklyTrackChart` + recommendations | `IPlaylistManager.CreatePlaylist` | LFM→JF | P2 |
| Tag Discovery | `user.getTopTags` + `tag.getTopTracks` | `IPlaylistManager.CreatePlaylist` | LFM→JF | P3 |
| **UI** |
| Recommendations Page | All recommendation APIs | `IHasWebPages` + API | Display | P2 |
| Stats Page | `user.getInfo` + `user.getTopArtists/Tracks` | `IHasWebPages` + API | Display | P3 |
| **Metadata** |
| Artist Images | `artist.getInfo` | `IRemoteImageProvider` | LFM→JF | P3 |
| Album Images | `album.getInfo` | `IRemoteImageProvider` | LFM→JF | P3 |

### Configuration Options Summary

| Setting | Scope | Type | Default |
|---------|-------|------|---------|
| **Global** |
| API Key | Admin | string | Required |
| Rate Limit | Admin | int | 1 req/sec |
| **Per-User: Scrobbling** |
| Enable | User | bool | true |
| Min Duration (sec) | User | int | 30 |
| Duplicate Window (min) | User | int | 5 |
| **Per-User: Favorites** |
| Sync to Last.fm | User | bool | true |
| Import from Last.fm | User | bool | true |
| Conflict Resolution | User | enum | NewestWins |
| **Per-User: Import** |
| Import Play Counts | User | bool | false |
| Play Count Strategy | User | enum | Max |
| Import Last Played | User | bool | false |
| **Per-User: Playlists** |
| Auto-Generate | User | bool | false |
| Playlist Types | User | flags | SimilarArtists |
| Max Tracks | User | int | 50 |
| Update Frequency | User | enum | Weekly |
