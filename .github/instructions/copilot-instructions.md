# Jellyfin Last.fm Plugin - Copilot Instructions

**Purpose**: Scrobble music to Last.fm, sync loved tracks, generate smart playlists  
**Type**: C# .NET 9.0 Jellyfin Plugin  
**Target**: Jellyfin 10.11.6  
**License**: GPL-2.0

---

## 📚 Documentation

| File | Description |
|------|-------------|
| [STATUS.md](STATUS.md) | Implementation status & architecture |
| [lastfm-api.instructions.md](lastfm-api.instructions.md) | Last.fm API reference |
| [jellyfin-api.instructions.md](jellyfin-api.instructions.md) | Jellyfin API reference |
| [api-cross-reference.instructions.md](api-cross-reference.instructions.md) | API mapping |
| [csharp-patterns.md](csharp-patterns.md) | Async/await, LINQ, logging |
| [csharp-security.md](csharp-security.md) | Password handling, HTTP, API keys |
| [development-workflow.md](development-workflow.md) | Building, versioning, CI/CD |

---

## 🚀 Quick Reference

### Build
```bash
dotnet build Jellyfin.Plugin.Lastfm -c Release
```

### Project Structure
```
Jellyfin.Plugin.Lastfm/
├── Plugin.cs                    # Entry point
├── PluginServiceRegistrator.cs  # DI registration
├── Services/                    # Business logic
├── Handlers/                    # Event handlers
├── Queue/                       # Offline scrobble queue
├── ScheduledTasks/              # Background jobs
├── Providers/                   # Image providers
└── Configuration/               # Settings + UI
```

---

## 🔑 Key Facts

| Property | Value |
|----------|-------|
| Plugin ID | `5e7fe7f0-b048-429e-a431-b1a7e69c930d` |
| Scrobble Rules | 30s minimum, 50% OR 4min threshold |
| MD5 Signature | **Lowercase hex** (required by Last.fm) |
| MusicBrainz ID | Recording MBID preferred, Track MBID fallback |
| Rate Limit | ~1000 requests/minute |

---

## 📖 Event Flow

```
PlaybackStart → track.updateNowPlaying → Last.fm
PlaybackStopped → validate (30s/50%/4min) → track.scrobble → Last.fm
UserDataSaved (IsFavorite) → track.love/unlove → Last.fm
```

---

## ⚠️ Before Updating Jellyfin Target

Check [development-workflow.md](development-workflow.md#jellyfin-update-checklist) for full checklist.
