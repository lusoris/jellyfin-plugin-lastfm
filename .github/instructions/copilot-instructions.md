# Jellyfin Last.fm Plugin - Copilot Instructions

**Purpose**: Scrobble music to Last.fm, sync loved tracks, generate smart playlists  
**Type**: C# .NET 9.0 Jellyfin Plugin  
**Target**: Jellyfin 10.11.6  
**License**: GPL-2.0

---

## 📚 Documentation Index

> **Modular Structure**: Instructions are split into focused modules. Load only what you need.

### 🚀 Getting Started
| Index | Modules |
|-------|---------|
| **[ide-setup.instructions.md](ide-setup.instructions.md)** | → [ide/package-managers.md](ide/package-managers.md) (apt, brew, dnf, pacman, winget, choco...) |
| | → [ide/shell-config.md](ide/shell-config.md) (bash, zsh, fish, PowerShell, nushell) |
| | → [ide/ide-vscode.md](ide/ide-vscode.md), [ide/ide-zed.md](ide/ide-zed.md), [ide/ide-rider.md](ide/ide-rider.md), [ide/ide-neovim.md](ide/ide-neovim.md) |
| [workflow/development-workflow.md](workflow/development-workflow.md) | Build, versioning, CI/CD |
| [workflow/testing.instructions.md](workflow/testing.instructions.md) | xUnit, Moq, coverage |

### 📡 API References
| Index | Modules |
|-------|---------|
| **[jellyfin-api.index.md](jellyfin-api.index.md)** | → [api/jellyfin-interfaces.md](api/jellyfin-interfaces.md) (ISessionManager, ILibraryManager) |
| | → [api/jellyfin-playback.md](api/jellyfin-playback.md) (events, scrobble logic) |
| | → [api/jellyfin-audio.md](api/jellyfin-audio.md) (Audio, MBIDs, queries) |
| | → [api/jellyfin-userdata.md](api/jellyfin-userdata.md) (favorites, play counts) |
| [lastfm-api.instructions.md](lastfm-api.instructions.md) | Last.fm API (scrobble, auth, imports) |
| [api-cross-reference.instructions.md](api-cross-reference.instructions.md) | Jellyfin ↔ Last.fm mapping |

### 🏗️ Architecture
| File | Description |
|------|-------------|
| [jellyfin-architecture.md](jellyfin-architecture.md) | Jellyfin plugin lifecycle, DI |
| [jellyfin-models.md](jellyfin-models.md) | Audio, User, UserData types |
| [jellyfin-configuration.md](jellyfin-configuration.md) | Plugin config system |
| [emby/README.md](emby/README.md) | Emby instructions (⏸️ On Hold) |
| [STATUS.md](STATUS.md) | Implementation status |
| [OPTIMIZATIONS.md](OPTIMIZATIONS.md) | Performance improvements |

### ✅ Code Quality
| File | Description |
|------|-------------|
| [csharp/csharp-patterns.md](csharp/csharp-patterns.md) | Performance, async, LoggerMessage |
| [csharp/csharp-security.md](csharp/csharp-security.md) | Security, strict mode, validation |
| [snyk_rules.instructions.md](snyk_rules.instructions.md) | Security scanning |
| [OPTIMIZATIONS.md](OPTIMIZATIONS.md) | Caching, deduplication, batch operations |

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
See [ide-setup.instructions.md](ide-setup.instructions.md) for VS Code, Zed, or Rider configuration.

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

1. **Read** [csharp/csharp-patterns.md](csharp/csharp-patterns.md) for performance patterns
2. **Read** [csharp/csharp-security.md](csharp/csharp-security.md) for security requirements
3. **Verify** build passes with 0 warnings (`TreatWarningsAsErrors=true`)
4. **Run** Snyk scan if adding new dependencies

## ⚠️ Before Updating Jellyfin Target

Check [development-workflow.md](workflow/development-workflow.md#jellyfin-update-checklist) for the full checklist.
