# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added - Core Architecture
- **Multi-Server Support**: Shared core library (`Lastfm.Scrobbler.Core`) for code reuse across platforms
- **Platform Adapters**: Clean separation via `IMediaServerAdapter`, `IPlaybackEventProvider`, `IFavoriteManager`
- **Polyfills**: C# 11 features (`init`, `required`) for .NET Standard 2.0 compatibility

### Added - Emby Plugin (netstandard2.0)
- Complete Emby plugin implementation targeting MediaBrowser.Server.Core 4.8.0.80
- **Scrobbling**: Now Playing updates and scrobble queue with eligibility checks (30s/50%/4min rules)
- **Favorites Sync**: Bidirectional love/unlove synchronization
- **Loved Tracks Import**: Scheduled task with pagination (1000 tracks/page)
- **User Mapping**: JellyfinUserId (Guid) matched against Emby User.Name (string)
- **MusicBrainz Matching**: Recording ID lookup with fallback to artist+track name

### Added - Jellyfin Plugin (net9.0)
- Complete Jellyfin plugin with advanced features
- **Smart Playlists**: 5 strategies (Similar Artists/Tracks, Rediscover, Mixtape, Tag Discovery)
- **REST API**: Authentication, status, queue management endpoints
- **Metadata Providers**: Artist and album image providers
- **Performance Optimizations**: Caching layer, batch operations, string interning
- **Custom UI**: Recommendations and statistics pages

### Added - Testing & Quality
- **Unit Tests**: 9 tests covering scrobble eligibility and signature generation
- **Code Coverage**: Coverlet integration with HTML reports (ReportGenerator)
- **Strict Analysis**: TreatWarningsAsErrors=true for Jellyfin plugin
- **Security**: Snyk integration in CI/CD pipeline

### Fixed
- **MD5 Signature Bug**: Last.fm API requires lowercase hex (was uppercase)
- **Emby API Compatibility**: Fixed Session.UserId (string vs Guid), method signatures, UserItemData persistence

### Changed
- **Build System**: Multi-targeting for .NET 8.0 and .NET Standard 2.0
- **JSON Serialization**: System.Text.Json with source generators (replacing Newtonsoft.Json)
- **Logging**: LoggerMessage source generators for performance
- **Documentation**: 4,000+ LOC comprehensive guides (API, patterns, security, IDE setup)

### CI/CD
- GitHub Actions: Build, test, lint, security scan
- Release automation: Tag-based releases (jellyfin-*, emby-*, plex-*)
- Manifest generation: Automatic updates to manifest.json

[Unreleased]: https://github.com/lusoris/jellyfin-plugin-lastfm/compare/v1.0.0...HEAD
