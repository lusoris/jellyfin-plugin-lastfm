# Clean-Room Rewrite Roadmap

## Overview

Complete rewrite of the Jellyfin Last.fm Plugin with:
- GPL-2.0 License
- Modern C# best practices (.NET 9.0)
- Improved architecture
- New features (playcount sync)

## Phase 1: Foundation (Priority: Critical)

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
  - [ ] Rate limiting (1 req/sec)
- [ ] `LastfmAuthService` for session management
- [ ] Request/Response DTOs with `System.Text.Json` attributes

### 1.3 Signature Generation
- [ ] `ISignatureGenerator` interface
- [ ] MD5 signature implementation (properly disposed)
- [ ] Unit tests for signature generation

## Phase 2: Core Features (Priority: High)

### 2.1 Scrobbling
- [ ] `IScrobbleService` interface
- [ ] Scrobble validation logic:
  - [ ] Track > 30 seconds
  - [ ] Played > 50% OR > 4 minutes
- [ ] Duplicate detection with time-based cache
- [ ] Batch scrobbling support (up to 50)
- [ ] Offline queue with persistence

### 2.2 Now Playing
- [ ] `track.updateNowPlaying` implementation
- [ ] Debounce rapid updates

### 2.3 Love/Unlove Sync
- [ ] `track.love` / `track.unlove` implementation
- [ ] Bidirectional sync:
  - [ ] Jellyfin → Last.fm (on favorite change)
  - [ ] Last.fm → Jellyfin (scheduled task)

## Phase 3: New Features (Priority: Medium)

### 3.1 Playcount Sync ⭐ NEW
- [ ] `user.getTopTracks` implementation
- [ ] Scheduled task to sync playcounts
- [ ] Match tracks via MusicBrainz ID or fuzzy name matching
- [ ] Update Jellyfin `UserData.PlayCount`

### 3.2 Recent Tracks Import
- [ ] `user.getRecentTracks` implementation
- [ ] Import historical scrobbles to Jellyfin

### 3.3 Metadata Provider Improvements
- [ ] Artist images from `artist.getInfo`
- [ ] Album images from `album.getInfo`
- [ ] Better caching strategy

## Phase 4: Quality & Polish (Priority: Medium)

### 4.1 Configuration UI
- [ ] Modern config page (Vue.js or vanilla)
- [ ] Connection test button
- [ ] Sync status display
- [ ] Per-user settings

### 4.2 Logging & Diagnostics
- [ ] Structured logging with proper levels
- [ ] Metrics/counters for:
  - [ ] Scrobbles sent/failed
  - [ ] API calls made
  - [ ] Sync operations

### 4.3 Error Handling
- [ ] Graceful degradation on API errors
- [ ] User-friendly error messages
- [ ] Automatic retry for transient failures

## Phase 5: Testing & Documentation (Priority: High)

### 5.1 Unit Tests
- [ ] API client tests with mocked HTTP
- [ ] Signature generation tests
- [ ] Scrobble validation logic tests
- [ ] Service layer tests

### 5.2 Integration Tests
- [ ] End-to-end scrobble flow
- [ ] Auth flow tests

### 5.3 Documentation
- [ ] README with features list
- [ ] Configuration guide
- [ ] API credits with Last.fm logo
- [ ] Contributing guide

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Jellyfin Plugin Host                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ ServerEntry │  │ Scheduled   │  │ Metadata Providers  │  │
│  │ Point       │  │ Tasks       │  │ (Artist/Album)      │  │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │
│         │                │                     │             │
│         ▼                ▼                     ▼             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                    Service Layer                         ││
│  │  ┌──────────────┐ ┌──────────────┐ ┌─────────────────┐  ││
│  │  │ IScrobble    │ │ ILoveSync    │ │ IPlaycountSync  │  ││
│  │  │ Service      │ │ Service      │ │ Service         │  ││
│  │  └──────┬───────┘ └──────┬───────┘ └────────┬────────┘  ││
│  └─────────┼────────────────┼──────────────────┼───────────┘│
│            │                │                  │             │
│            ▼                ▼                  ▼             │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                  ILastfmApiClient                        ││
│  │  ┌────────────────────────────────────────────────────┐ ││
│  │  │ - Scrobble()        - GetLovedTracks()             │ ││
│  │  │ - UpdateNowPlaying() - GetTopTracks()              │ ││
│  │  │ - LoveTrack()       - GetRecentTracks()            │ ││
│  │  │ - UnloveTrack()     - Authenticate()               │ ││
│  │  └────────────────────────────────────────────────────┘ ││
│  └─────────────────────────────────────────────────────────┘│
│                              │                               │
│                              ▼                               │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              HttpClient (Singleton via DI)               ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │   Last.fm API       │
                    │ ws.audioscrobbler.  │
                    │       com           │
                    └─────────────────────┘
```

## C# Best Practices Checklist

### Dependency Injection
- [ ] All services registered via `IServiceCollection`
- [ ] `IHttpClientFactory` instead of `new HttpClient()`
- [ ] `IMemoryCache` from DI instead of own instance
- [ ] `ILogger<T>` for typed logging

### Async/Await
- [ ] No `async void` (except event handlers with try/catch)
- [ ] `ConfigureAwait(false)` in library code
- [ ] Proper cancellation token support
- [ ] No `.Result` or `.Wait()` blocking calls

### Resource Management
- [ ] `IDisposable` implemented where needed
- [ ] `using` statements/declarations for disposables
- [ ] `IAsyncDisposable` for async cleanup

### Error Handling
- [ ] Specific exception types
- [ ] No empty catch blocks
- [ ] Logging before rethrowing

### Performance
- [ ] `ValueTask` for hot paths
- [ ] `Span<T>` / `Memory<T>` where applicable
- [ ] Object pooling for frequent allocations
- [ ] Avoid LINQ in tight loops

### Code Style
- [ ] Nullable reference types enabled
- [ ] File-scoped namespaces
- [ ] Primary constructors where appropriate
- [ ] Records for DTOs

## File Structure (New)

```
Jellyfin.Plugin.Lastfm/
├── Plugin.cs
├── PluginServiceRegistrator.cs
├── Api/
│   ├── ILastfmApiClient.cs
│   ├── LastfmApiClient.cs
│   └── Models/
│       ├── Requests/
│       │   ├── ScrobbleRequest.cs
│       │   ├── NowPlayingRequest.cs
│       │   └── ...
│       └── Responses/
│           ├── ScrobbleResponse.cs
│           └── ...
├── Services/
│   ├── IScrobbleService.cs
│   ├── ScrobbleService.cs
│   ├── ILoveSyncService.cs
│   ├── LoveSyncService.cs
│   ├── IPlaycountSyncService.cs
│   └── PlaycountSyncService.cs
├── Configuration/
│   ├── PluginConfiguration.cs
│   └── configPage.html
├── ScheduledTasks/
│   ├── SyncLovedTracksTask.cs
│   └── SyncPlaycountsTask.cs
├── EventHandlers/
│   ├── PlaybackEventHandler.cs
│   └── UserDataEventHandler.cs
└── Utils/
    ├── SignatureGenerator.cs
    └── TrackMatcher.cs
```

## Timeline Estimate

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| Phase 1 | ~4h | None |
| Phase 2 | ~6h | Phase 1 |
| Phase 3 | ~4h | Phase 2 |
| Phase 4 | ~3h | Phase 2 |
| Phase 5 | ~3h | All |
| **Total** | **~20h** | |

## Questions to Resolve

1. **Playcount sync direction**: Last.fm → Jellyfin only, or bidirectional?
   - Recommendation: Last.fm → Jellyfin (Last.fm is source of truth for plays)

2. **Offline scrobble queue**: File-based or SQLite?
   - Recommendation: JSON file (simpler, no extra dependency)

3. **Track matching**: Strict MusicBrainz only, or fuzzy name matching?
   - Recommendation: MusicBrainz preferred, fuzzy as fallback
