# Development Workflow & Building

## Building the Project

```bash
# Restore dependencies
dotnet restore Jellyfin.Plugin.Lastfm

# Build (Debug)
dotnet build Jellyfin.Plugin.Lastfm

# Build (Release)
dotnet build Jellyfin.Plugin.Lastfm -c Release

# Clean build
dotnet clean Jellyfin.Plugin.Lastfm && dotnet build Jellyfin.Plugin.Lastfm
```

---

## Version Management

### Version Scheme
**Format**: `{jellyfin_major}.{jellyfin_minor}.{jellyfin_patch}.{plugin_revision}`

Examples:
- `10.11.6.0` - Initial release for Jellyfin 10.11.6
- `10.11.6.1` - Bugfix release for Jellyfin 10.11.6
- `10.12.0.0` - Initial release for Jellyfin 10.12.0

### Assembly Version (Jellyfin.Plugin.Lastfm.csproj)
```xml
<PropertyGroup>
    <Version>10.11.6.0</Version>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyVersion>10.11.6.0</AssemblyVersion>
</PropertyGroup>
```

### Plugin Metadata (build.yaml)
```yaml
version: 10.11.6.0             # Plugin version (matches Jellyfin + revision)
targetAbi: 10.11.6.0           # Jellyfin ABI target
```

### Release Tagging
```bash
git tag -a 10.11.6.0 -m "Release 10.11.6.0"
git push origin 10.11.6.0
```

**Tag Format**: `{jellyfin_version}.{revision}` (e.g., `10.11.6.0`, `10.11.6.1`)

---

## Release Workflow (GitHub Actions)

### Trigger: Tag Push
```bash
git tag -a 10.11.6.1 && git push origin 10.11.6.1
```

### Automatic Steps:
1. Workflow `create-github-release.yml` triggers
2. Builds release: `dotnet build -c Release`
3. Creates ZIP: `lastfm_10.11.6.1.zip`
4. Updates manifest: `manifest.json` with new version
5. Creates GitHub Release with ZIP

### Manual Alternative:
```bash
# Build release manually
dotnet build Jellyfin.Plugin.Lastfm -c Release

# Create ZIP from bin/Release/net9.0/
cd Jellyfin.Plugin.Lastfm/bin/Release/net9.0
zip -j ../../../lastfm_10.11.6.0.zip Jellyfin.Plugin.Lastfm.dll
```

---

## Testing Strategy

### Current State
- No formal unit test project
- Manual integration testing via Jellyfin instance
- Test audio files in `tests/` directory

### What Should Be Tested

```csharp
// API Client (mocked HTTP)
public class LastfmApiClientTests
{
    [Test]
    public async Task RequestSession_WithValidCredentials_ReturnsSessionKey()
    {
        var response = await _apiClient.RequestSession("user", "pass");
        Assert.NotNull(response.SessionKey);
    }
    
    [Test]
    public async Task Scrobble_WithValidTrack_SubmitsRequest()
    {
        // Verify MD5 signature, request format
    }
    
    [Test]
    public async Task GetLovedTracks_WithPagination_ReturnsAllTracks()
    {
        // Test pagination handling
    }
}

// Scrobbling Logic
public class ServerEntryPointTests
{
    [Test]
    public void PlaybackStopped_WithInsufficientDuration_DoesNotScrobble()
    {
        // Track < 30 seconds
    }
    
    [Test]
    public void PlaybackStopped_WithInsufficientPlaytime_DoesNotScrobble()
    {
        // Played < 4 minutes AND < 50%
    }
    
    [Test]
    public void PlaybackStopped_WithinDuplicateWindow_DoesNotScrobble()
    {
        // Same artist+track within 15 seconds
    }
}

// Configuration
public class PluginConfigurationTests
{
    [Test]
    public void Configuration_Persists_LastfmUsers()
    {
        // Save and reload config
    }
}
```

---

## Security Scanning (Snyk)

### Pre-Commit Checklist

**Before pushing changes:**

1. Run security scan:
```bash
snyk code scan Jellyfin.Plugin.Lastfm/
```

2. Review findings:
   - HTTP client usage (no direct instantiation)
   - JSON deserialization (XXE, JSON bombs)
   - String formatting (no user input concatenation)
   - Password/key handling (never log, never transmit)

3. Fix High/Critical issues

4. Rescan to verify:
```bash
snyk code scan Jellyfin.Plugin.Lastfm/
```

5. Commit and push only when clean

---

## CI/CD Workflows

### build-plugin.yaml (On main branch)
- Triggers: Push to main, pull requests
- Builds and validates plugin
- Checks compile errors

### create-github-release.yml (On tag)
- Triggers: Push new tag (v*.*.*)
- Builds release
- Creates GitHub release
- Generates manifest entry
- Uploads to blob storage

---

## Local Development Setup

### Prerequisites
- .NET 9.0 SDK
- VS Code or Visual Studio
- Jellyfin instance for testing

### Jellyfin Development Instance

```bash
# Using Docker (recommended for testing)
docker run -d \
  -p 8096:8096 \
  -v jellyfin-config:/config \
  -v jellyfin-media:/media \
  jellyfin/jellyfin:latest
```

### Install Plugin Locally

```bash
# Build release
dotnet build Jellyfin.Plugin.Lastfm -c Release

# Copy to Jellyfin plugins directory
cp Jellyfin.Plugin.Lastfm/bin/Release/net9.0/Jellyfin.Plugin.Lastfm.dll \
   ~/.config/jellyfin/plugins/Jellyfin.Plugin.Lastfm.dll

# Restart Jellyfin (plugin auto-loads)
```

---

## Debugging

### VS Code Debug Configuration (.vscode/launch.json)

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/bin/Release/net9.0/Jellyfin.Plugin.Lastfm.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
            "serverReadyAction": {
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
                "uriFormat": "%s"
            },
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

### Logging in Development

```csharp
// Enable debug logging to see more details
_logger.LogDebug("Playback stopped at {0}s of {1}s", 
    playedSeconds, durationSeconds);

// Check Jellyfin logs:
// ~/.local/share/jellyfin/logs/
```

---

## Commit Message Format

```
feat: Add configurable scrobbling thresholds
fix: Prevent duplicate scrobbles in 15s window
docs: Update scrobbling rules documentation
chore: Upgrade Jellyfin.Controller to 10.11.6
style: Format code with prettier
test: Add unit tests for API client
perf: Cache metadata queries
refactor: Extract common validation logic
```

**Prefixes**:
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `chore` - Build, dependencies, tooling
- `style` - Code formatting
- `test` - Tests
- `perf` - Performance
- `refactor` - Code restructuring

---

## Creating a PR

1. Create feature branch:
```bash
git checkout -b feature/my-feature
```

2. Make changes and test locally

3. Run security scan:
```bash
snyk code scan Jellyfin.Plugin.Lastfm/
```

4. Commit with descriptive message:
```bash
git commit -m "feat: Detailed description"
```

5. Push to origin:
```bash
git push origin feature/my-feature
```

6. Create PR against `main` branch

7. Wait for CI/CD checks and review

---

**Related**:
- [snyk_rules.instructions.md](snyk_rules.instructions.md) - Security scanning
- [csharp-security.md](csharp-security.md) - Security patterns
