# Emby Plugin Build Instructions

Build script for Emby.Plugin.Lastfm targeting .NET Framework 4.8.

## Prerequisites

```bash
# .NET SDK 8.0+ required (supports net48 target)
dotnet --version  # Must be 8.0+
```

## Build Commands

### Clean Build
```bash
dotnet clean Emby.Plugin.Lastfm
dotnet build Emby.Plugin.Lastfm -c Release
```

### Build Output
```
Emby.Plugin.Lastfm/bin/Release/net48/
├── Emby.Plugin.Lastfm.dll
├── Lastfm.Scrobbler.Core.dll
├── Lastfm.Scrobbler.Abstractions.dll
└── System.Text.Json.dll
```

### Create Distribution ZIP
```bash
cd Emby.Plugin.Lastfm/bin/Release/net48
zip -r ../../../../emby-lastfm-plugin.zip \
  Emby.Plugin.Lastfm.dll \
  Lastfm.Scrobbler.Core.dll \
  Lastfm.Scrobbler.Abstractions.dll \
  System.Text.Json.dll
```

## Installation

### Manual Installation

1. Extract ZIP to Emby plugins directory:
   - Windows: `%AppData%\Emby-Server\plugins\LastfmScrobbler\`
   - Linux: `~/.config/emby-server/plugins/LastfmScrobbler/`
   - Docker: `/config/plugins/LastfmScrobbler/`

2. Restart Emby Server

3. Verify installation:
   - Navigate to: Dashboard → Plugins
   - "Last.fm Scrobbler" should appear in the list

### Configuration

1. Go to: Dashboard → Plugins → Last.fm Scrobbler
2. Click "Configure"
3. Add Last.fm user credentials
4. Save settings

## Troubleshooting

### Build Errors

**Error**: `The target "net48" is not recognized`
```bash
# Install .NET Framework targeting pack
# Windows: Visual Studio Installer → Modify → Individual Components → .NET Framework 4.8 targeting pack
# Linux: Install mono-complete
```

**Error**: `MediaBrowser.Server.Core not found`
```bash
# Check NuGet source configuration
dotnet nuget list source

# Add Emby NuGet feed if missing
dotnet nuget add source https://nuget.emby.media/v3/index.json -n EmbyNuget
```

### Runtime Errors

**Plugin doesn't load**
- Check Emby logs: `%AppData%\Emby-Server\logs\server-*.log`
- Verify .NET Framework 4.8 is installed: `reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release`
- Ensure all DLLs are in the same directory

**Missing dependencies**
```bash
# Copy missing System.* DLLs from NuGet packages
cp ~/.nuget/packages/system.text.json/8.0.5/lib/net462/System.Text.Json.dll \
   Emby.Plugin.Lastfm/bin/Release/net48/
```

## Development

### Debug Build
```bash
dotnet build Emby.Plugin.Lastfm -c Debug
```

### Watch Mode (auto-rebuild)
```bash
dotnet watch --project Emby.Plugin.Lastfm build
```

### Code Analysis
```bash
# Run analyzers
dotnet build Emby.Plugin.Lastfm /p:TreatWarningsAsErrors=true

# Check documentation coverage
dotnet build Emby.Plugin.Lastfm /p:GenerateDocumentationFile=true
```

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Build Emby Plugin
  run: |
    dotnet restore Emby.Plugin.Lastfm
    dotnet build Emby.Plugin.Lastfm -c Release
    
- name: Package Plugin
  run: |
    cd Emby.Plugin.Lastfm/bin/Release/net48
    zip -r $GITHUB_WORKSPACE/emby-plugin.zip *.dll
    
- name: Upload Artifact
  uses: actions/upload-artifact@v3
  with:
    name: emby-plugin
    path: emby-plugin.zip
```

---

**Related:**
- [README.md](README.md) - Installation guide
- [../.github/instructions/emby/](../.github/instructions/emby/) - Development documentation
