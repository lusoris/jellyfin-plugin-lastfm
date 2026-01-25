# JetBrains Rider Setup

Configuration for JetBrains Rider.

## Installation

| Platform | Command |
|----------|---------|
| brew | `brew install --cask rider` |
| winget | `winget install JetBrains.Rider` |
| choco | `choco install rider` |
| Toolbox | JetBrains Toolbox App (recommended) |

## Initial Setup

1. **Open solution**: `File → Open → Jellyfin.Plugin.Lastfm.sln`
2. Rider auto-detects .NET SDK
3. Wait for indexing to complete

## Recommended Plugins

| Plugin | Purpose |
|--------|---------|
| .NET Core User Secrets | Secure config |
| EditorConfig | Code style |
| Key Promoter X | Learn shortcuts |

Install via: `Settings → Plugins → Marketplace`

## Settings

### Code Style

`Settings → Editor → Code Style → C#`:
- ✅ Enable EditorConfig support
- ✅ Reformat code on save

### Toolset

`Settings → Build, Execution, Deployment → Toolset`:
- .NET CLI: Auto-detected
- MSBuild: Bundled (recommended)

## Run Configuration

Create `Run/Debug Configuration`:
- **Type**: .NET Project
- **Project**: `Jellyfin.Plugin.Lastfm`
- **Configuration**: Release

## Keyboard Shortcuts

| Action | Windows/Linux | macOS |
|--------|---------------|-------|
| Build | `Ctrl+Shift+B` | `Cmd+Shift+B` |
| Run | `Shift+F10` | `Ctrl+R` |
| Debug | `Shift+F9` | `Ctrl+D` |
| Find Usages | `Alt+F7` | `Opt+F7` |
| Refactor | `Ctrl+Shift+R` | `Ctrl+T` |

## Advantages

- ✅ Excellent refactoring tools
- ✅ Built-in decompiler
- ✅ Superior debugging experience
- ✅ Database tools
- ✅ Git integration

---

**Related:** [ide/ide-vscode.md](ide/ide-vscode.md) | [ide/ide-neovim.md](ide/ide-neovim.md)
