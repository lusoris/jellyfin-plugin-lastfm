# Jellyfin Last.fm Plugin - AI Coding Agent Index

**Purpose**: Scrobble music playback to Last.fm, sync loved tracks, provide metadata enrichment

**Type**: C# .NET 9.0 Jellyfin Plugin | **Target**: Jellyfin 10.11.6 | **Framework**: .NET 9.0
**Repository**: https://github.com/lusoris/jellyfin-plugin-lastfm | **Latest Release**: 10.11.6.0

---

## 📚 Documentation Index

### **Core Jellyfin & Plugin Architecture**
- [**jellyfin-architecture.md**](jellyfin-architecture.md) - Plugin lifecycle, DI container, event system, REST endpoints
- [**jellyfin-models.md**](jellyfin-models.md) - Audio, User, UserData types and usage patterns
- [**jellyfin-configuration.md**](jellyfin-configuration.md) - Plugin configuration, persistence, config access

### **API & Integration**
- [**lastfm-api-instructions.md**](lastfm-api-instructions.md) - Last.fm API endpoints, authentication, scrobbling rules, error handling
- [**csharp-patterns.md**](csharp-patterns.md) - Async/await, LINQ, logging, null-coalescing, collections

### **Security & Development**
- [**csharp-security.md**](csharp-security.md) - Password handling, HTTP security, API keys, input validation, JSON safety
- [**development-workflow.md**](development-workflow.md) - Building, versioning, testing, CI/CD, **Jellyfin update checklist**
- [**snyk_rules.instructions.md**](snyk_rules.instructions.md) - Security scanning requirements

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
├── Plugin.cs                          # Entry point
├── PluginServiceRegistrator.cs        # DI registration
├── ServerEntryPoint.cs                # IHostedService (scrobbling logic)
├── Api/                               # HTTP client + Last.fm endpoints
├── Models/                            # Request/response types
├── Providers/                         # Metadata providers
├── ScheduledTasks/                    # Loved tracks sync
└── Configuration/                     # Config models + web UI
```

---

## ⚡ Common Tasks

**Need to...**

- **Understand plugin lifecycle?** → [jellyfin-architecture.md](jellyfin-architecture.md)
- **Check event system or DI?** → [jellyfin-architecture.md](jellyfin-architecture.md)
- **Access User/Audio models?** → [jellyfin-models.md](jellyfin-models.md)
- **Modify configuration?** → [jellyfin-configuration.md](jellyfin-configuration.md)
- **Call Last.fm API?** → [lastfm-api-instructions.md](lastfm-api-instructions.md)
- **Use async/await, LINQ, logging?** → [csharp-patterns.md](csharp-patterns.md)
- **Handle passwords, HTTP, API keys?** → [csharp-security.md](csharp-security.md)
- **Build release, run tests, scan security?** → [development-workflow.md](development-workflow.md)
- **Update to new Jellyfin version?** → [development-workflow.md](development-workflow.md#jellyfin-update-checklist)

---

## 🔑 Key Facts

- **Plugin ID**: `5e7fe7f0-b048-429e-a431-b1a7e69c930d`
- **Version Scheme**: `{jellyfin_version}.{revision}` (e.g., `10.11.6.0`, `10.11.6.1`)
- **DI Pattern**: Inject `IHttpClientFactory`, `ISessionManager`, `IUserDataManager`
- **Event Handlers**: `async void` is CORRECT for Jellyfin event subscriptions (not a code smell here!)
- **Config**: XML-based, auto-persists via `Plugin.Instance.PluginConfiguration`
- **Scrobbling**: Requires 30s+ duration, 4min OR 50% playtime, 15s duplicate window
- **Auth**: Exchange password for permanent `SessionKey`, never store password
- **Last.fm API**: MD5 signature required on all requests, ~1000 req/min rate limit

---

## ⚠️ Before Updating Jellyfin Target

**ALWAYS check before bumping targetAbi:**
1. Jellyfin release notes: https://github.com/jellyfin/jellyfin/releases
2. GitHub issues for plugin compatibility
3. Upstream repo (jesseward) for patches

See [development-workflow.md](development-workflow.md#jellyfin-update-checklist) for full checklist.

---

## 📖 Architecture Overview (Simplified)

```
User plays track in Jellyfin
    ↓
ISessionManager.PlaybackStopped event fires
    ↓
ServerEntryPoint.PlaybackStopped() called
    ↓
Validate: duration ≥ 30s, playtime ≥ 4min OR 50%, not duplicate
    ↓
Call Last.fm API: track.scrobble
    ↓
Log success/error
```

---

**Read full documentation**: Start with [jellyfin-architecture.md](jellyfin-architecture.md), then refer to specific guides as needed.

**Related**: See each file's "Related" section for cross-references.
