# Jellyfin Last.fm Plugin - Copilot Instructions

**Purpose**: Scrobble music to Last.fm, sync loved tracks, generate smart playlists  
**Type**: C# .NET 9.0 Jellyfin Plugin  
**Target**: Jellyfin 10.11.6  
**License**: GPL-2.0

---

## 📚 Documentation Index

### Getting Started
| File | Description |
|------|-------------|
| [ide-setup.md](ide-setup.md) | **Start here** - VS Code, Zed, Rider setup |
| [development-workflow.md](development-workflow.md) | Building, versioning, CI/CD |
| [STATUS.md](STATUS.md) | Implementation status & architecture |

### API References
| File | Description |
|------|-------------|
| [lastfm-api.instructions.md](lastfm-api.instructions.md) | Last.fm API (scrobble, nowplaying, etc.) |
| [jellyfin-api.instructions.md](jellyfin-api.instructions.md) | Jellyfin plugin interfaces |
| [jellyfin-architecture.md](jellyfin-architecture.md) | Plugin lifecycle & DI |
| [jellyfin-models.md](jellyfin-models.md) | Audio, User, UserData types |
| [jellyfin-configuration.md](jellyfin-configuration.md) | Plugin config system |
| [api-cross-reference.instructions.md](api-cross-reference.instructions.md) | Last.fm ↔ Jellyfin mapping |

### Code Quality
| File | Description |
|------|-------------|
| [csharp-patterns.md](csharp-patterns.md) | Performance, async, LoggerMessage |
| [csharp-security.md](csharp-security.md) | Security, strict mode, validation |
| [snyk_rules.instructions.md](snyk_rules.instructions.md) | Security scanning |

---

## 🚀 Quick Start

### Prerequisites
```bash
# .NET 9.0 SDK required
dotnet --version  # Must be 9.0+
```

### Build
```bash
dotnet build Jellyfin.Plugin.Lastfm -c Release
```

### IDE Setup
See [ide-setup.md](ide-setup.md) for VS Code, Zed, or Rider configuration.

---

## 🔑 Key Facts

| Property | Value |
|----------|-------|
| Plugin ID | `5e7fe7f0-b048-429e-a431-b1a7e69c930d` |
| .NET Version | 9.0 |
| Jellyfin Target | 10.11.6 |
| Analyzers | `AnalysisLevel=latest-recommended` |
| Strict Mode | `TreatWarningsAsErrors=true` |

### Scrobbling Rules
- Minimum track length: **30 seconds**
- Scrobble threshold: **50% of track** OR **4 minutes** (whichever first)

### Critical Implementation Details
| Aspect | Requirement |
|--------|-------------|
| MD5 Signature | **Lowercase hex** (`Convert.ToHexStringLower`) |
| MusicBrainz ID | Recording MBID preferred, Track MBID fallback |
| Rate Limit | ~1000 requests/minute |
| Classes | Always `sealed` unless inheritance needed |
| Logging | Use `[LoggerMessage]` source generators |

---

## 📖 Event Flow

```
PlaybackStart → track.updateNowPlaying → Last.fm
PlaybackStopped → validate (30s/50%/4min) → track.scrobble → Last.fm
UserDataSaved (IsFavorite) → track.love/unlove → Last.fm
```

---

## 📁 Project Structure

```
Jellyfin.Plugin.Lastfm/
├── Plugin.cs                    # Entry point, BasePlugin<T>
├── PluginServiceRegistrator.cs  # DI registration
├── Services/                    # Business logic
│   ├── LastfmApiClient.cs      # HTTP client, retry, rate limiting
│   ├── ScrobbleService.cs      # Validation rules
│   ├── SignatureGenerator.cs   # MD5 signatures (lowercase!)
│   └── TrackMatcherService.cs  # MusicBrainz + fuzzy matching
├── Handlers/                    # Jellyfin event handlers
│   ├── PlaybackEventHandler.cs # Scrobble + now playing
│   └── UserDataEventHandler.cs # Favorites sync
├── Queue/                       # Offline scrobble queue
├── ScheduledTasks/              # Background jobs
├── Providers/                   # Image providers
├── Api/                         # REST endpoints
└── Configuration/               # Settings + web UI
```

---

## ⚠️ Before Changing Code

1. **Read** [csharp-patterns.md](csharp-patterns.md) for performance patterns
2. **Read** [csharp-security.md](csharp-security.md) for security requirements
3. **Verify** build passes with 0 warnings (`TreatWarningsAsErrors=true`)
4. **Run** Snyk scan if adding new dependencies

## ⚠️ Before Updating Jellyfin Target

Check [development-workflow.md](development-workflow.md#jellyfin-update-checklist) for the full checklist.
