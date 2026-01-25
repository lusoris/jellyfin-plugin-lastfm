# Emby Plugin Build & Installation

## Building

```bash
# Build Emby plugin
dotnet build Emby.Plugin.Lastfm -c Release

# Output: Emby.Plugin.Lastfm/bin/Release/net48/Emby.Plugin.Lastfm.dll
```

## Installation

### Manual Installation

1. Build the plugin (see above)
2. Copy `Emby.Plugin.Lastfm.dll` to your Emby plugins directory:
   - Windows: `%AppData%\Emby-Server\plugins\`
   - Linux: `~/.config/emby-server/plugins/`
   - Docker: `/config/plugins/` (mount point)
3. Copy dependencies:
   - `Lastfm.Scrobbler.Core.dll`
   - `Lastfm.Scrobbler.Abstractions.dll`
4. Restart Emby Server

### Plugin Structure

```
plugins/
├── Emby.Plugin.Lastfm.dll
├── Lastfm.Scrobbler.Core.dll
├── Lastfm.Scrobbler.Abstractions.dll
└── System.Text.Json.dll (if not already in Emby)
```

## Development

### Prerequisites

- .NET SDK 8.0+ (for building with net48 target)
- Emby Server 4.8+
- Visual Studio 2022 or VS Code with C# extension

### Running Tests

```bash
# Currently no Emby-specific tests
# Core functionality tested via Jellyfin.Plugin.Lastfm.Tests
dotnet test
```

## Configuration

After installation, navigate to:
- Emby Dashboard → Plugins → Last.fm Scrobbler

## Troubleshooting

### Plugin doesn't load
- Check Emby logs: `%AppData%\Emby-Server\logs\`
- Verify all DLLs are in plugins directory
- Ensure .NET Framework 4.8 is installed

### Scrobbles not appearing on Last.fm
- Check Last.fm API credentials in plugin config
- Verify network connectivity
- Check Emby logs for errors

---

**Related:**
- [../.github/instructions/emby/emby-architecture.md](../.github/instructions/emby/emby-architecture.md) - Architecture
- [../.github/instructions/emby/emby-api.md](../.github/instructions/emby/emby-api.md) - API reference
- [../.github/instructions/emby/emby-patterns.md](../.github/instructions/emby/emby-patterns.md) - Code patterns
