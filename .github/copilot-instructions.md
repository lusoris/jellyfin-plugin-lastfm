# Jellyfin Last.fm Plugin - AI Coding Agent Instructions

## Project Overview
This is a C# plugin for Jellyfin that enables Last.fm music scrobbling, metadata enrichment, and loved-tracks synchronization. The plugin runs as a hosted service, monitors user playback in Jellyfin, and synchronizes data bidirectionally with Last.fm's API.

**Target**: Jellyfin 10.11.6 | **Framework**: .NET 9.0 | **Repository**: https://github.com/lusoris/jellyfin-plugin-lastfm

## Repository Structure & Setup

### Quick Start
```bash
git clone https://github.com/lusoris/jellyfin-plugin-lastfm.git
cd jellyfin-plugin-lastfm
dotnet build Jellyfin.Plugin.Lastfm/Jellyfin.Plugin.Lastfm.csproj
```

### Key Directories
- **Jellyfin.Plugin.Lastfm/** - Main plugin code
  - **Api/** - Last.fm API client (BaseLastfmApiClient, LastfmApiClient)
  - **Models/** - Data models and request/response types
  - **Providers/** - Metadata providers (artist, album, image)
  - **ScheduledTasks/** - ImportLastfmData task for syncing loved tracks
  - **Configuration/** - Plugin configuration (XML-based, embedded HTML UI)
  - **Utils/** - Helper functions for user management and string handling
- **.github/workflows/** - CI/CD: build-plugin.yaml (main branch), create-github-release.yml (tag-based releases)
- **manifest.json** - Plugin repository manifest (10.11.6 ABI)
- **build.yaml** - Plugin metadata and versioning

## Architecture & Key Components

### Plugin Lifecycle
1. **Entry Points**:
   - `Plugin.cs` - Implements `BasePlugin<PluginConfiguration>` and `IHasWebPages`
   - `PluginServiceRegistrator.cs` - Registers `ServerEntryPoint` as `IHostedService`
   - `ServerEntryPoint.cs` - Main hosted service handling playback events

2. **Service Registration** (Jellyfin dependency injection):
```csharp
public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
{
    serviceCollection.AddHostedService<ServerEntryPoint>();
}
```

3. **Configuration**: XML file persisted via Jellyfin's configuration system
   - Stores array of `LastfmUser` objects with SessionKey and user options (Scrobble, SyncFavourites, AlternativeMode)
   - Web UI at `Configuration/configPage.html`

### Three Core Workflows

#### 1. Real-time Scrobbling (ServerEntryPoint.cs)
**Key Constants**:
- Minimum track duration: 30 seconds
- Minimum playtime: 4 minutes (or 50% played)
- Prevents duplicate scrobbles within 15 seconds

**Event Handlers** (⚠️ Current Issue: `async void` - should be `async Task`):
- `PlaybackStart()` - Sends "now playing" to Last.fm
- `PlaybackStopped()` - Scrobbles when conditions met
- `UserDataSaved()` - Syncs favorite/love status

**Flow**: Session event → validation (user exists, has SessionKey, feature enabled) → API call → logging

#### 2. Session Authentication (Api/RestApi.cs)
- **Endpoint**: `POST /Lastfm/Login`
- **Input**: `LastFMUser` DTO (Username, Password)
- **Output**: `MobileSessionResponse` with permanent `SessionKey`
- **Critical**: Never stores passwords—only session keys persisted in XML config

#### 3. Data Import (ScheduledTasks/ImportLastfmData.cs)
- Implements `IScheduledTask`
- Syncs all users' Last.fm loved tracks into local Jellyfin library
- Sets `Plugin.Syncing = true` flag to prevent real-time scrobbles during import (prevents conflicts)
- Matches Last.fm tracks to local items by artist/album/title
- Marks matched items as favorites in Jellyfin

### API Communication Pattern

**Base Client**: `BaseLastfmApiClient.cs`
- Handles request/response serialization with JSON
- Computes MD5 signature for Last.fm API authentication (`Helpers.AppendSignature()`)
- URL: `http://ws.audioscrobbler.com/2.0/?format=json`

**Derived Client**: `LastfmApiClient.cs`
- Specific methods: `RequestSession()`, `Scrobble()`, `NowPlaying()`, `GetLovedTracks()`, `LoveTrack()`

**Request/Response Models** (Models/Requests & Models/Responses):
- All inherit from `BaseRequest`/`BaseResponse`
- Support JSON serialization with number handling for string-quoted numbers
- Include Last.fm-specific error handling

### Metadata Providers

- **LastfmArtistProvider.cs** - Fetches artist images and biography
- **LastfmAlbumProvider.cs** - Fetches album images
- **LastfmImageProvider.cs** - Image fetching utility
- Uses MusicBrainz IDs for matching

## Critical Patterns & Best Practices

### Data Flow
1. User configures Last.fm credentials via web UI → POST to `/Lastfm/Login` → SessionKey stored in XML
2. Jellyfin playback events → `ServerEntryPoint` event handlers → validate SessionKey → API call
3. Scheduled import task → fetches all user loved tracks → matches to library → marks favorites
4. Both scrobbles and favorites require valid SessionKey

### Jellyfin Integration Points
- **Dependencies**: `IHttpClientFactory`, `IUserManager`, `ILibraryManager`, `ISessionManager`, `IUserDataManager`
- **User Identification**: `Guid MediaBrowserUserId` links `LastfmUser` to Jellyfin user
- **Media Type Filtering**: Check `if (e.Item is not Audio)` before processing
- **Configuration Access**: `Plugin.Instance.PluginConfiguration`
- **Logging**: `ILogger<T>` from Jellyfin's DI container

### Async Pattern Compliance
✅ Good: `ConfigureAwait(false)` used throughout
✅ Good: Proper HttpClient factory usage
✅ Correct: `async void` event handlers in ServerEntryPoint (required for event subscription to Jellyfin's `PlaybackStart`, `PlaybackStopped`, `UserDataSaved` events which expect void delegates)

## Build & Testing

**Build**: 
```bash
dotnet build
```

**Target Framework**: net9.0 (matches Jellyfin 10.11.6)

**Tests**: 
- Currently minimal—test audio files in `tests/` directory (Nightmares On Wax album)
- No unit test project; integration testing via Jellyfin instance

**Docker**: See `Dockerfile` for containerized plugin testing

**CI/CD**:
- **Trigger**: Push to `main` branch or pull requests
- **Release**: Tag-based builds create GitHub releases and update Azure blob manifest
- **Manifest**: manifest.json in repo root, also generated during release workflow

## Required Secrets (GitHub Actions)
- `GITHUB_TOKEN` - Pre-supplied by GitHub
- `AZURE_STORAGE_CONTAINER_NAME` - For manifest uploads
- `AZURE_STORAGE_CONNECTION_STRING` - For manifest uploads

## Dependencies & Versions

```xml
<PackageReference Include="System.Memory" Version="4.5.4" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.4" />
<PackageReference Include="Jellyfin.Controller" Version="10.*-*" /> <!-- Now 10.11.6 -->
```

## Known Issues & Considerations

1. **Branch Migration**: Repository recently migrated from `master` to `main` (Jan 2026)
- **Archive Notice**: This is a maintained fork of the original jesseward/jellyfin-plugin-lastfm repository
3. **Testing**: No dedicated test project; consider adding unit tests for API client
4. **Security**: Passwords never stored; only session keys persisted (✓ Secure)

## Performance Notes
- Uses connection pooling via HttpClientFactory
- Metadata providers run async without blocking playback thread
- Scrobble deduplication prevents excessive API calls
- Scheduled import respects cancellation tokens

## Snyk Security Requirements
- Always run `snyk_code_scan` for new C# code
- Address security hotspots and taint vulnerabilities before PR merge
- Focus on: Password handling, HTTP client usage, JSON deserialization

## Key Files to Reference
- [ServerEntryPoint.cs](Jellyfin.Plugin.Lastfm/ServerEntryPoint.cs) - Scrobbling logic with proper event handler pattern
- [BaseLastfmApiClient.cs](Jellyfin.Plugin.Lastfm/Api/BaseLastfmApiClient.cs) - API signature generation
- [PluginConfiguration.cs](Jellyfin.Plugin.Lastfm/Configuration/PluginConfiguration.cs) - Config model
- [LastfmUser.cs](Jellyfin.Plugin.Lastfm/Models/LastfmUser.cs) - User model with SessionKey
- [ImportLastfmData.cs](Jellyfin.Plugin.Lastfm/ScheduledTasks/ImportLastfmData.cs) - Sync logic
- [build.yaml](build.yaml) - Plugin metadata (version 10.11.6-0, ABI 10.11.6.0)
