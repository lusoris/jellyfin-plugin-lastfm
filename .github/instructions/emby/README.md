# Emby Instructions Index

> **Status**: ⏸️ On Hold - Emby implementation deferred

Emby-specific documentation for Last.fm plugin development.

## Current Status

Emby support is **not yet implemented**. The plugin currently targets:
- **Jellyfin 10.11.6** (`.NET 9.0`) - ✅ Fully supported

## Emby Implementation Plan

When Emby support is added, it will require:

### Technical Requirements
- **Separate project**: `Emby.Plugin.Lastfm.csproj`
- **Target Framework**: `.NET Framework 4.8`
- **Emby SDK**: Via NuGet (MediaBrowser.Server.Core)
- **Entry Point**: `IServerEntryPoint` (not `IHostedService`)
- **HTTP Client**: `IHttpClient` (not `IHttpClientFactory`)

### Shared Components
The following components are platform-agnostic and can be reused:
- `Lastfm.Scrobbler.Core` - API client, scrobble logic
- `Lastfm.Scrobbler.Abstractions` - Interfaces/DTOs
- Adapter pattern for platform differences

### Key Differences from Jellyfin

| Aspect | Jellyfin | Emby |
|--------|----------|------|
| Runtime | .NET 9.0 | .NET Framework 4.8 |
| Entry Point | `IHostedService` | `IServerEntryPoint` |
| HTTP | `IHttpClientFactory` | `IHttpClient` |
| DI | `Microsoft.Extensions.DependencyInjection` | Custom container |
| License | GPL-2.0 (Open Source) | Proprietary |

## Why Deferred?

1. **Focus**: Current priority is Jellyfin 10.11.6 compatibility
2. **Architecture**: Abstractions layer is ready for multi-platform
3. **Resources**: Requires separate build pipeline, testing infrastructure
4. **Demand**: Waiting for community feedback on Emby interest

## Documentation (Ready for Implementation)

Complete Emby documentation available:
- [emby-api.md](emby-api.md) - Emby API reference (ISessionManager, ILibraryManager, IHttpClient)
- [emby-architecture.md](emby-architecture.md) - Plugin lifecycle & structure (Entry points, DI, Events)
- [emby-patterns.md](emby-patterns.md) - .NET Framework 4.8 patterns (async/await, LINQ, HTTP client)

**Related:** [../jellyfin-architecture.md](../jellyfin-architecture.md) | [../workflow/development-workflow.md](../workflow/development-workflow.md)

---

**Interested in Emby support?** Open an issue at [github.com/jesseward/jellyfin-plugin-lastfm/issues](https://github.com/jesseward/jellyfin-plugin-lastfm/issues)
