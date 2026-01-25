# Jellyfin Last.fm Plugin - Copilot Instructions

**Purpose**: Scrobble music playback to Last.fm, sync loved tracks, generate smart playlists

**Type**: C# .NET 9.0 Jellyfin Plugin  
**Target**: Jellyfin 10.11.6  
**License**: GPL-2.0 (clean-room rewrite in progress)  
**Repository**: https://github.com/lusoris/jellyfin-plugin-lastfm

---

## 📚 Documentation Index

### API References
| File | Description |
|------|-------------|
| [lastfm-api.instructions.md](lastfm-api.instructions.md) | Complete Last.fm API reference |
| [jellyfin-api.instructions.md](jellyfin-api.instructions.md) | Complete Jellyfin API reference |
| [api-cross-reference.instructions.md](api-cross-reference.instructions.md) | Last.fm ↔ Jellyfin API mapping |

### Jellyfin Plugin Development
| File | Description |
|------|-------------|
| [jellyfin-architecture.md](jellyfin-architecture.md) | Plugin lifecycle, DI, events |
| [jellyfin-models.md](jellyfin-models.md) | Audio, User, UserData types |
| [jellyfin-configuration.md](jellyfin-configuration.md) | Plugin configuration |

### C# Best Practices
| File | Description |
|------|-------------|
| [csharp-patterns.md](csharp-patterns.md) | Async/await, LINQ, logging |
| [csharp-security.md](csharp-security.md) | Password handling, HTTP, API keys |

### Development & Planning
| File | Description |
|------|-------------|
| [development-workflow.md](development-workflow.md) | Building, versioning, CI/CD |
| [ROADMAP.md](ROADMAP.md) | Clean-room rewrite roadmap |
| [snyk_rules.instructions.md](snyk_rules.instructions.md) | Security scanning |

---

## 🚀 Quick Start

### Build
```bash
git clone https://github.com/lusoris/jellyfin-plugin-lastfm.git
cd jellyfin-plugin-lastfm
dotnet build Jellyfin.Plugin.Lastfm/Jellyfin.Plugin.Lastfm.csproj
```

### Project Structure
```
Jellyfin.Plugin.Lastfm/
├── Plugin.cs                          # Entry point, IHasWebPages
├── PluginServiceRegistrator.cs        # DI registration
│
├── Services/                          # Business logic (interfaces + impl)
│   ├── ILastfmApiClient.cs           # Last.fm HTTP client interface
│   ├── LastfmApiClient.cs            # HTTP calls, retry, rate limiting
│   ├── IScrobbleService.cs           # Scrobble validation interface
│   ├── ScrobbleService.cs            # 30s/50%/4min rules, dedup
│   ├── ITrackMatcherService.cs       # Track matching interface
│   ├── TrackMatcherService.cs        # MusicBrainz ID + fuzzy matching
│   ├── ISignatureGenerator.cs        # MD5 signature interface
│   └── SignatureGenerator.cs         # Last.fm API signature
│
├── Handlers/                          # Jellyfin event handlers
│   ├── PlaybackEventHandler.cs       # IHostedService: playback events
│   └── UserDataEventHandler.cs       # IHostedService: favorites sync
│
├── Models/                            # DTOs and domain models
│   ├── LastfmUser.cs                 # User + session key
│   ├── ScrobbleInfo.cs               # Track info for scrobbling
│   ├── Requests/                     # Last.fm API request DTOs
│   │   ├── ScrobbleRequest.cs
│   │   ├── NowPlayingRequest.cs
│   │   └── ...
│   └── Responses/                    # Last.fm API response DTOs
│       ├── ScrobbleResponse.cs
│       ├── LovedTracksResponse.cs
│       └── ...
│
├── Configuration/                     # Plugin settings
│   ├── PluginConfiguration.cs        # Global + per-user settings
│   └── configPage.html               # Vanilla JS config UI
│
├── ScheduledTasks/                    # Background jobs
│   ├── SyncLovedTracksTask.cs        # Import loved tracks from Last.fm
│   └── ProcessScrobbleQueueTask.cs   # Flush offline queue
│
└── Queue/                             # Offline scrobble queue
    ├── IScrobbleQueue.cs             # Queue interface
    └── ScrobbleQueue.cs              # JSON file persistence
```

---

## ⚡ Common Tasks

| Need to... | See... |
|------------|--------|
| Understand plugin lifecycle | [jellyfin-architecture.md](jellyfin-architecture.md) |
| Access User/Audio models | [jellyfin-models.md](jellyfin-models.md) |
| Modify configuration | [jellyfin-configuration.md](jellyfin-configuration.md) |
| Call Last.fm API | [lastfm-api.instructions.md](lastfm-api.instructions.md) |
| Map features to APIs | [api-cross-reference.instructions.md](api-cross-reference.instructions.md) |
| Use async/await, LINQ | [csharp-patterns.md](csharp-patterns.md) |
| Handle passwords, HTTP | [csharp-security.md](csharp-security.md) |
| Build release, CI/CD | [development-workflow.md](development-workflow.md) |
| Plan new features | [ROADMAP.md](ROADMAP.md) |

---

## 🔑 Key Facts

| Property | Value |
|----------|-------|
| Plugin ID | `5e7fe7f0-b048-429e-a431-b1a7e69c930d` |
| Version Scheme | `{jellyfin_version}.{revision}` (e.g., `10.11.6.1`) |
| DI Services | `IHttpClientFactory`, `ISessionManager`, `IUserDataManager`, `ILibraryManager` |
| Event Handlers | `async void` is correct for Jellyfin event subscriptions |
| Config Storage | XML-based, auto-persists via `Plugin.Instance.PluginConfiguration` |

### Scrobbling Rules
- Minimum track duration: 30 seconds
- Scrobble threshold: 4 minutes OR 50% playtime
- Duplicate window: 15 seconds default

### Last.fm Authentication
- Exchange password for permanent `SessionKey` (one-time)
- Never store passwords
- MD5 signature required on all write requests
- Rate limit: ~1000 requests/minute

---

## ⚠️ Before Updating Jellyfin Target

**ALWAYS check before bumping targetAbi:**

1. [Jellyfin release notes](https://github.com/jellyfin/jellyfin/releases)
2. [GitHub issues](https://github.com/jellyfin/jellyfin/issues?q=plugin) for plugin compatibility
3. [Upstream repo](https://github.com/jesseward/jellyfin-plugin-lastfm) for patches

See [development-workflow.md](development-workflow.md#jellyfin-update-checklist) for full checklist.

---

## 📖 Event Flow (Simplified)

```
User plays track in Jellyfin
    ↓
ISessionManager.PlaybackStart → track.updateNowPlaying
    ↓
... playing ...
    ↓
ISessionManager.PlaybackStopped
    ↓
Validate: duration > 30s, played > 50% OR > 4min
    ↓
track.scrobble → Last.fm
```

```
User favorites a track in Jellyfin
    ↓
IUserDataManager.UserDataSaved (IsFavorite = true)
    ↓
track.love → Last.fm
```

```
Scheduled task runs
    ↓
user.getLovedTracks ← Last.fm
    ↓
Match tracks in Jellyfin library
    ↓
Set UserItemData.IsFavorite = true
```
