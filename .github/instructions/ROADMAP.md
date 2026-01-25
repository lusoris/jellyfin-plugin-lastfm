# Clean-Room Rewrite Roadmap

## Overview

Complete rewrite of the Jellyfin Last.fm Plugin with:
- GPL-2.0 License
- Modern C# best practices (.NET 9.0)
- Improved architecture
- **Full bi-directional sync** (everything configurable!)
- **Smart playlist generation** from Last.fm recommendations
- **Custom UI pages** under Music section

## Features Matrix

### Bi-Directional Sync Features

| Feature | JF → LFM | LFM → JF | Configurable Options |
|---------|:--------:|:--------:|----------------------|
| **Scrobbles** | ✅ | - | Enable, min duration, duplicate window, batch size |
| **Now Playing** | ✅ | - | Enable, update interval |
| **Loved/Favorites** | ✅ | ✅ | Enable, sync direction, conflict resolution |
| **Play Count** | - | ✅ | Enable, sync strategy (add/replace/max) |
| **Last Played Date** | - | ✅ | Enable, overwrite existing |
| **Historical Import** | - | ✅ | Enable, date range, max tracks |

### Playlist & Discovery Features

| Feature | Description | Configurable Options |
|---------|-------------|----------------------|
| **Similar Artists Playlist** | Tracks from artists similar to your favorites | Enable, source (top artists/recent), limit |
| **Similar Tracks Playlist** | Tracks similar to recently played | Enable, seed tracks count, limit |
| **Rediscover Favorites** | Old loved tracks you haven't played recently | Enable, age threshold, limit |
| **Weekly Mixtape** | Based on your weekly chart + recommendations | Enable, auto-update, limit |
| **Tag/Genre Discovery** | Tracks from your top tags | Enable, tag source, limit |

### Configuration Hierarchy

```
Global Settings (Admin)
├── API Configuration
│   ├── API Key (required)
│   └── Rate Limiting (requests/sec)
├── Default Sync Settings
│   ├── Scrobbling defaults
│   ├── Love sync defaults
│   └── Import defaults
└── Playlist Generation Defaults

Per-User Settings
├── Last.fm Account
│   ├── Username
│   └── Session Key
├── Scrobbling
│   ├── Enable scrobbling
│   ├── Minimum duration (seconds)
│   └── Duplicate detection window (minutes)
├── Sync JF → Last.fm
│   ├── Sync favorites to loved tracks
│   └── Real-time sync
├── Sync Last.fm → JF
│   ├── Import loved tracks as favorites
│   ├── Import play counts (add/replace/max)
│   ├── Import last played dates
│   └── Sync interval (hours)
├── Playlist Generation
│   ├── Enable auto-generation
│   ├── Playlist types (similar artists, similar tracks, etc.)
│   ├── Max tracks per playlist
│   ├── Update frequency
│   └── Naming template
└── Conflict Resolution
    ├── Favorite conflicts: Last.fm wins / Jellyfin wins / Newest wins
    └── Play count: Higher wins / Last.fm wins / Jellyfin wins
```

---

## Phase 1: Foundation (~6h)

### 1.1 License & Project Setup
- [ ] Add GPL-2.0 LICENSE file
- [ ] Update README with new license
- [ ] Create new project structure
- [ ] Setup dependency injection properly

### 1.2 Core API Client
- [ ] `ILastfmApiClient` interface
- [ ] `LastfmApiClient` implementation with:
  - [ ] Singleton HttpClient via DI
  - [ ] Static/cached `JsonSerializerOptions`
  - [ ] Proper async/await patterns
  - [ ] Retry logic with exponential backoff
  - [ ] Rate limiting (configurable, default 1 req/sec)
- [ ] `LastfmAuthService` for session management
- [ ] Request/Response DTOs with `System.Text.Json` attributes

### 1.3 Signature Generation
- [ ] `ISignatureGenerator` interface
- [ ] MD5 signature implementation (properly disposed)
- [ ] Unit tests for signature generation

### 1.4 Configuration System
- [ ] `PluginConfiguration` with all settings
- [ ] `LastfmUserConfiguration` for per-user settings
- [ ] Configuration validation
- [ ] Migration from old config format

---

## Phase 2: Core Sync Features (~8h)

### 2.1 Scrobbling (JF → LFM)
- [ ] `IScrobbleService` interface
- [ ] Scrobble validation logic:
  - [ ] Track > 30 seconds (configurable)
  - [ ] Played > 50% OR > 4 minutes
- [ ] Duplicate detection with configurable time window
- [ ] Batch scrobbling support (up to 50)
- [ ] Offline queue with persistence (JSON file)

### 2.2 Now Playing (JF → LFM)
- [ ] `track.updateNowPlaying` implementation
- [ ] Debounce rapid updates (configurable interval)

### 2.3 Love/Unlove Sync (Bidirectional)
- [ ] `track.love` / `track.unlove` implementation
- [ ] JF → LFM: On favorite change event
- [ ] LFM → JF: Scheduled import task
- [ ] Conflict resolution strategies

### 2.4 Track Matching Service
- [ ] `ITrackMatcherService` interface
- [ ] Match strategies:
  1. MusicBrainz Recording ID (exact)
  2. MusicBrainz Track ID (exact)
  3. Artist + Track name (fuzzy)
  4. Artist + Album + Track (fuzzy)
- [ ] Configurable match strictness
- [ ] Match result caching

---

## Phase 3: Data Import Features (~6h)

### 3.1 Play Count Import (LFM → JF)
- [ ] `user.getTopTracks` implementation (paginated)
- [ ] Sync strategies:
  - **Add**: Add Last.fm count to Jellyfin count
  - **Replace**: Overwrite Jellyfin count
  - **Max**: Use higher of the two
- [ ] Scheduled task with configurable interval
- [ ] Progress reporting

### 3.2 Last Played Date Import (LFM → JF)
- [ ] `user.getRecentTracks` implementation (paginated)
- [ ] Only update if newer (configurable)
- [ ] Batch updates to Jellyfin database

### 3.3 Historical Scrobble Import (LFM → JF)
- [ ] Full scrobble history import
- [ ] Date range filtering
- [ ] Resume capability for large libraries

---

## Phase 4: Smart Playlist Generation (~8h)

### 4.1 Playlist Service
- [ ] `IPlaylistGeneratorService` interface
- [ ] Integration with `IPlaylistManager`
- [ ] Playlist metadata (description, cover art)
- [ ] Auto-update existing playlists

### 4.2 Similar Artists Playlist
- [ ] `artist.getSimilar` API implementation
- [ ] Find local tracks from similar artists
- [ ] Configurable: seed from top artists or recent plays

### 4.3 Similar Tracks Playlist
- [ ] `track.getSimilar` API implementation
- [ ] Find similar tracks in local library
- [ ] Weighted by play history

### 4.4 Rediscover Favorites Playlist
- [ ] `user.getLovedTracks` API
- [ ] Filter by last played date (e.g., not played in 6 months)
- [ ] Prioritize highly-scrobbled old favorites

### 4.5 Weekly Mixtape Playlist
- [ ] `user.getWeeklyTrackChart` API
- [ ] Mix of recent favorites + similar discoveries
- [ ] Auto-update weekly

### 4.6 Tag-Based Discovery Playlist
- [ ] `user.getTopTags` API
- [ ] `tag.getTopTracks` API
- [ ] Find local tracks matching user's top tags

---

## Phase 5: Metadata Providers (~4h)

### 5.1 Artist Provider Improvements
- [ ] `artist.getInfo` for biography and images
- [ ] `artist.getSimilar` for related artists metadata
- [ ] Better caching strategy (configurable TTL)

### 5.2 Album Provider Improvements
- [ ] `album.getInfo` for album details
- [ ] Album art fallback from Last.fm
- [ ] Tag/genre enrichment

---

## Phase 6: UI & Quality (~4h)

### 6.1 Configuration Page (Modern)
- [ ] Modern config page (Vue.js or vanilla JS)
- [ ] Connection test button with status
- [ ] Per-user settings management
- [ ] Sync status dashboard

### 6.2 Custom Pages (Under Music)
- [ ] **Last.fm Recommendations** page
  - Display similar artists
  - Display recommended tracks
  - Quick "Add to playlist" action
- [ ] **Last.fm Statistics** page
  - Scrobble count
  - Top artists/tracks visualization
  - Listening history chart

### 6.3 Logging & Diagnostics
- [ ] Structured logging with proper levels
- [ ] Metrics dashboard:
  - Scrobbles sent/failed
  - Sync operations status
  - API call statistics

### 6.4 Error Handling
- [ ] Graceful degradation on API errors
- [ ] User-friendly error messages
- [ ] Automatic retry for transient failures
- [ ] Circuit breaker for persistent failures

---

## Phase 7: Testing & Documentation (~4h)

### 7.1 Unit Tests
- [ ] API client tests with mocked HTTP
- [ ] Signature generation tests
- [ ] Scrobble validation logic tests
- [ ] Track matcher tests
- [ ] Playlist generator tests

### 7.2 Integration Tests
- [ ] End-to-end scrobble flow
- [ ] Auth flow tests
- [ ] Sync task tests

### 7.3 Documentation
- [ ] README with full features list
- [ ] Configuration guide with examples
- [ ] Troubleshooting guide
- [ ] API credits with Last.fm logo
- [ ] Contributing guide

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         Jellyfin Plugin Host                              │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌─────────────────────┐ │
│  │ ServerEntry│  │ Scheduled  │  │ API        │  │ Custom Web Pages    │ │
│  │ Point      │  │ Tasks      │  │ Controllers│  │ (Recommendations,   │ │
│  │            │  │            │  │            │  │  Statistics)        │ │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └──────────┬──────────┘ │
│        │               │               │                    │            │
│        ▼               ▼               ▼                    ▼            │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                         Service Layer                              │  │
│  │ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐ ┌──────────────┐ │  │
│  │ │ IScrobble   │ │ ILoveSync   │ │ IPlaycount   │ │ IPlaylist    │ │  │
│  │ │ Service     │ │ Service     │ │ SyncService  │ │ Generator    │ │  │
│  │ └──────┬──────┘ └──────┬──────┘ └──────┬───────┘ └──────┬───────┘ │  │
│  │        │               │               │                │         │  │
│  │        └───────────────┴───────────────┴────────────────┘         │  │
│  │                                │                                   │  │
│  │                     ┌──────────▼──────────┐                       │  │
│  │                     │  ITrackMatcher      │                       │  │
│  │                     │  Service            │                       │  │
│  │                     └──────────┬──────────┘                       │  │
│  └────────────────────────────────┼──────────────────────────────────┘  │
│                                   │                                      │
│                                   ▼                                      │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                       ILastfmApiClient                             │  │
│  │ ┌──────────────────────────────────────────────────────────────┐  │  │
│  │ │ Scrobble Methods          │ User Data Methods                │  │  │
│  │ │ - Scrobble()              │ - GetLovedTracks()               │  │  │
│  │ │ - UpdateNowPlaying()      │ - GetTopTracks()                 │  │  │
│  │ │                           │ - GetRecentTracks()              │  │  │
│  │ ├───────────────────────────┼──────────────────────────────────┤  │  │
│  │ │ Love Methods              │ Recommendation Methods           │  │  │
│  │ │ - LoveTrack()             │ - GetSimilarArtists()            │  │  │
│  │ │ - UnloveTrack()           │ - GetSimilarTracks()             │  │  │
│  │ │                           │ - GetTopTags()                   │  │  │
│  │ │                           │ - GetWeeklyChart()               │  │  │
│  │ └──────────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                   │                                      │
│  ┌────────────────────────────────┼──────────────────────────────────┐  │
│  │  Jellyfin Services             │                                   │  │
│  │  - IPlaylistManager ◄──────────┤                                   │  │
│  │  - IUserDataManager ◄──────────┤                                   │  │
│  │  - ILibraryManager  ◄──────────┘                                   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                         ┌─────────────────────┐
                         │     Last.fm API     │
                         │  ws.audioscrobbler  │
                         │        .com         │
                         └─────────────────────┘
```

---

## File Structure

```
Jellyfin.Plugin.Lastfm/
├── Plugin.cs
├── PluginServiceRegistrator.cs
│
├── Api/
│   ├── ILastfmApiClient.cs
│   ├── LastfmApiClient.cs
│   ├── LastfmAuthService.cs
│   └── Models/
│       ├── Requests/
│       │   ├── ScrobbleRequest.cs
│       │   ├── NowPlayingRequest.cs
│       │   ├── LoveRequest.cs
│       │   ├── GetSimilarArtistsRequest.cs
│       │   ├── GetSimilarTracksRequest.cs
│       │   ├── GetTopTracksRequest.cs
│       │   ├── GetLovedTracksRequest.cs
│       │   ├── GetRecentTracksRequest.cs
│       │   ├── GetTopTagsRequest.cs
│       │   └── GetWeeklyChartRequest.cs
│       └── Responses/
│           ├── ScrobbleResponse.cs
│           ├── SimilarArtistsResponse.cs
│           ├── SimilarTracksResponse.cs
│           ├── TopTracksResponse.cs
│           ├── LovedTracksResponse.cs
│           ├── RecentTracksResponse.cs
│           ├── TopTagsResponse.cs
│           └── WeeklyChartResponse.cs
│
├── Services/
│   ├── Scrobbling/
│   │   ├── IScrobbleService.cs
│   │   ├── ScrobbleService.cs
│   │   └── OfflineScrobbleQueue.cs
│   ├── Sync/
│   │   ├── ILoveSyncService.cs
│   │   ├── LoveSyncService.cs
│   │   ├── IPlaycountSyncService.cs
│   │   └── PlaycountSyncService.cs
│   ├── Playlists/
│   │   ├── IPlaylistGeneratorService.cs
│   │   ├── PlaylistGeneratorService.cs
│   │   └── Strategies/
│   │       ├── SimilarArtistsStrategy.cs
│   │       ├── SimilarTracksStrategy.cs
│   │       ├── RediscoverFavoritesStrategy.cs
│   │       ├── WeeklyMixtapeStrategy.cs
│   │       └── TagDiscoveryStrategy.cs
│   └── Matching/
│       ├── ITrackMatcherService.cs
│       └── TrackMatcherService.cs
│
├── Configuration/
│   ├── PluginConfiguration.cs
│   ├── LastfmUserConfiguration.cs
│   └── configPage.html
│
├── Pages/
│   ├── recommendations.html
│   └── statistics.html
│
├── Controllers/
│   └── LastfmController.cs
│
├── ScheduledTasks/
│   ├── SyncLovedTracksTask.cs
│   ├── SyncPlaycountsTask.cs
│   └── GeneratePlaylistsTask.cs
│
├── EventHandlers/
│   ├── PlaybackEventHandler.cs
│   └── UserDataEventHandler.cs
│
├── Providers/
│   ├── LastfmArtistProvider.cs
│   ├── LastfmAlbumProvider.cs
│   └── LastfmImageProvider.cs
│
└── Utils/
    ├── SignatureGenerator.cs
    ├── StringHelper.cs
    └── RateLimiter.cs
```

---

## Timeline Estimate

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| Phase 1: Foundation | ~6h | None |
| Phase 2: Core Sync | ~8h | Phase 1 |
| Phase 3: Data Import | ~6h | Phase 2 |
| Phase 4: Playlists | ~8h | Phase 2, 3 |
| Phase 5: Providers | ~4h | Phase 1 |
| Phase 6: UI & Quality | ~4h | Phase 2, 4 |
| Phase 7: Testing | ~4h | All |
| **Total** | **~40h** | |

---

## Configuration Options Reference

### Global Settings

```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    // API Settings
    public string ApiKey { get; set; } = "";
    public int RateLimitRequestsPerSecond { get; set; } = 1;
    
    // Default Sync Settings
    public bool DefaultScrobblingEnabled { get; set; } = true;
    public int DefaultMinimumScrobbleDuration { get; set; } = 30;
    public int DefaultDuplicateDetectionWindow { get; set; } = 5;
    
    public bool DefaultSyncFavoritesToLoved { get; set; } = true;
    public bool DefaultImportLovedAsFavorites { get; set; } = true;
    public bool DefaultImportPlayCounts { get; set; } = false;
    public PlayCountSyncStrategy DefaultPlayCountStrategy { get; set; } = PlayCountSyncStrategy.Max;
    
    // Playlist Generation Defaults
    public bool DefaultAutoGeneratePlaylists { get; set; } = false;
    public int DefaultPlaylistMaxTracks { get; set; } = 50;
}
```

### Per-User Settings

```csharp
public class LastfmUserConfiguration
{
    public string Username { get; set; }
    public string SessionKey { get; set; }
    
    // Scrobbling
    public bool ScrobblingEnabled { get; set; } = true;
    public int MinimumScrobbleDuration { get; set; } = 30;
    public int DuplicateDetectionWindowMinutes { get; set; } = 5;
    
    // JF → Last.fm Sync
    public bool SyncFavoritesToLoved { get; set; } = true;
    public bool RealTimeFavoriteSync { get; set; } = true;
    
    // Last.fm → JF Sync
    public bool ImportLovedAsFavorites { get; set; } = true;
    public bool ImportPlayCounts { get; set; } = false;
    public PlayCountSyncStrategy PlayCountStrategy { get; set; } = PlayCountSyncStrategy.Max;
    public bool ImportLastPlayedDate { get; set; } = false;
    public int SyncIntervalHours { get; set; } = 24;
    
    // Playlist Generation
    public bool AutoGeneratePlaylists { get; set; } = false;
    public List<PlaylistType> EnabledPlaylistTypes { get; set; } = new();
    public int PlaylistMaxTracks { get; set; } = 50;
    public int PlaylistUpdateFrequencyHours { get; set; } = 168; // weekly
    public string PlaylistNamingTemplate { get; set; } = "Last.fm - {type}";
    
    // Conflict Resolution
    public ConflictResolution FavoriteConflictResolution { get; set; } = ConflictResolution.LastfmWins;
    public ConflictResolution PlayCountConflictResolution { get; set; } = ConflictResolution.HigherWins;
}

public enum PlayCountSyncStrategy { Add, Replace, Max }
public enum ConflictResolution { LastfmWins, JellyfinWins, NewestWins, HigherWins }
public enum PlaylistType { SimilarArtists, SimilarTracks, RediscoverFavorites, WeeklyMixtape, TagDiscovery }
```

---

## Homepage & UI Integration Ideas

### Custom Pages Under Music Section

Using `IHasWebPages` with `EnableInMainMenu = true` and `MenuSection = "music"`:

```csharp
yield return new PluginPageInfo
{
    Name = "LastfmForYou",
    DisplayName = "Last.fm For You",
    EmbeddedResourcePath = GetType().Namespace + ".Pages.foryou.html",
    EnableInMainMenu = true,
    MenuSection = "music",
    MenuIcon = "stars"
};
```

### Page Ideas

#### 1. "Last.fm For You" Page
Main recommendation hub showing:
- **Discover Artists** - Similar to your favorites (in your library)
- **Discover Tracks** - Similar to recently played
- **Quick Actions** - Generate playlist buttons
- Powered by `artist.getSimilar`, `track.getSimilar`

#### 2. "Listening Stats" Page
Visual statistics dashboard:
- Total scrobbles (lifetime, week, month)
- Top 5 artists with play counts
- Top 5 tracks with play counts
- Listening history timeline
- "You've listened to X hours this week"
- Powered by `user.getInfo`, `user.getTopArtists`, `user.getTopTracks`

#### 3. "Rediscover" Page
Surface old favorites:
- "Haven't heard in a while" section
- "Your most scrobbled, least played locally"
- "From your early Last.fm days"
- Powered by `user.getLovedTracks` + local `UserItemData.LastPlayedDate`

#### 4. "Weekly Report" Page
Auto-generated weekly summary:
- "Your week in music" stats
- Top artist/track of the week
- Discovery rate (new artists played)
- Powered by `user.getWeeklyArtistChart`, `user.getWeeklyTrackChart`

### API Endpoints for Pages

```csharp
[ApiController]
[Route("Lastfm")]
public class LastfmController : ControllerBase
{
    // For "For You" page
    [HttpGet("Recommendations/{userId}")]
    Task<LastfmRecommendationsDto> GetRecommendations(Guid userId)
    
    // For "Stats" page
    [HttpGet("Stats/{userId}")]
    Task<LastfmStatsDto> GetUserStats(Guid userId)
    
    // For "Stats" page - period-based
    [HttpGet("Stats/{userId}/Period/{period}")]
    Task<LastfmPeriodStatsDto> GetPeriodStats(Guid userId, string period)
    
    // For "Rediscover" page
    [HttpGet("Rediscover/{userId}")]
    Task<IEnumerable<Audio>> GetRediscoverTracks(Guid userId, int limit = 50)
    
    // For "Weekly Report" page
    [HttpGet("WeeklyReport/{userId}")]
    Task<WeeklyReportDto> GetWeeklyReport(Guid userId)
    
    // Action: Generate playlist
    [HttpPost("Playlists/Generate")]
    Task<PlaylistCreationResult> GeneratePlaylist(Guid userId, string strategy)
}
```

### Why Plugin Pages (Not Homepage Sections)

Jellyfin's `HomeSectionType` is an internal enum - plugins cannot add new section types. However:

1. **Plugin pages with `EnableInMainMenu = true`** appear in the navigation menu
2. **`MenuSection = "music"`** places them under the Music library section
3. **Full HTML/JS control** allows rich interactive UIs
4. **Custom API endpoints** provide all needed data

This is the recommended approach and matches how official Jellyfin plugins work.

### Alternative: Influence Home Sections Indirectly

While you can't add custom home sections, you can:

1. **"Continue Listening"** (ResumeAudio) - Create/update playlists that show here
2. **"Latest Media"** - If you add tracks to library, they appear here
3. **Play history** - Scrobble imports update `LastPlayedDate`, affecting suggestions

---

## Resolved Questions

1. **Playcount sync direction**: Last.fm → Jellyfin with configurable strategy (add/replace/max)

2. **Offline scrobble queue**: JSON file (simple, no extra dependency)

3. **Track matching**: MusicBrainz preferred, fuzzy name matching as fallback, configurable strictness

4. **Playlists vs Collections**: Use Playlists (better for music, ordered lists, per-user)

5. **Custom UI**: Plugin pages under Music section using `IHasWebPages` with `EnableInMainMenu = true`

6. **Homepage integration**: Cannot add custom home sections; use plugin pages in menu instead

7. **Cross-reference guide**: See `api-cross-reference.instructions.md` for complete API mapping

8. **UI Framework**: Plain HTML + vanilla JS (or bundled modern JS)
   - Jellyfin loads plugin pages as embedded HTML, NOT as React components
   - React is used internally by jellyfin-web, but plugins don't have access
   - Option: Build custom JS bundle with Vite and embed as `<script>` tag
   - Keep it simple for now: Modern vanilla JS with Jellyfin's `ApiClient`

---

## Development Approach

### Step-by-Step Implementation Order

```
Phase 1: Foundation (MUST DO FIRST)
├── 1.1 Project Setup
│   ├── Add GPL-2.0 LICENSE
│   ├── Clean project structure
│   └── Remove old code references
├── 1.2 API Client
│   ├── ILastfmApiClient interface
│   ├── HttpClient setup with DI
│   ├── Signature generation
│   └── Rate limiting
└── 1.3 Configuration
    ├── PluginConfiguration (global)
    ├── Per-user settings storage
    └── Basic config page

Phase 2: Core Scrobbling (Essential Feature)
├── 2.1 Event Handlers
│   ├── PlaybackStart → Now Playing
│   └── PlaybackStopped → Scrobble
├── 2.2 Scrobble Logic
│   ├── Duration validation (>30s, >50%)
│   ├── Duplicate detection
│   └── Offline queue (JSON)
└── 2.3 Authentication
    └── auth.getMobileSession flow

Phase 3: Bidirectional Favorites (High Value)
├── 3.1 JF → Last.fm
│   └── IsFavorite change → track.love/unlove
├── 3.2 Last.fm → JF
│   ├── user.getLovedTracks import
│   └── Scheduled task
└── 3.3 Conflict Resolution
    └── Configurable strategies

Phase 4: Play Count & History Import
├── 4.1 Play Count Sync
│   ├── user.getTopTracks fetch
│   ├── Match in library
│   └── Update UserItemData.PlayCount
└── 4.2 Last Played Date
    └── user.getRecentTracks → LastPlayedDate

Phase 5: Smart Playlists
├── 5.1 Playlist Service
│   ├── IPlaylistManager integration
│   └── Auto-update existing playlists
├── 5.2 Strategies (one at a time)
│   ├── Similar Artists
│   ├── Similar Tracks
│   ├── Rediscover Favorites
│   ├── Weekly Mixtape
│   └── Tag Discovery
└── 5.3 Configuration
    └── Per-strategy settings

Phase 6: Custom UI Pages
├── 6.1 Basic Pages
│   ├── Recommendations page
│   └── Statistics page
├── 6.2 API Endpoints
│   └── LastfmController with REST API
└── 6.3 Page Content
    ├── HTML structure
    ├── JS interactivity
    └── Jellyfin style integration

Phase 7: Polish & Testing
├── 7.1 Error Handling
├── 7.2 Logging
├── 7.3 Documentation
└── 7.4 Testing (optional)
```

### MVP Definition

**Minimum Viable Product (Phases 1-3):**
- ✅ Scrobbling works
- ✅ Now Playing works
- ✅ Favorites sync bidirectionally
- ✅ Basic configuration page
- ⏳ No playlists yet
- ⏳ No custom pages yet

This gets us a **working replacement** for the old plugin quickly, then we can add features incrementally.
