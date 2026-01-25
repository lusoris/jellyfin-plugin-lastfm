````markdown
# IDE Setup Guide

## Prerequisites (All IDEs)

### .NET SDK

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# Check installed version
dotnet --version  # Must be 9.0+

# Install on Linux (Ubuntu/Debian)
sudo apt install dotnet-sdk-9.0

# Install on macOS
brew install dotnet@9

# Add to PATH (if needed)
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# Check installed version
dotnet --version  # Must be 9.0+

# Install on Linux (Ubuntu/Debian)
sudo apt install dotnet-sdk-9.0

# Install on macOS
brew install dotnet@9

# Add to PATH (if needed) - add to ~/.config/fish/config.fish
set -gx DOTNET_ROOT $HOME/.dotnet
fish_add_path $DOTNET_ROOT $DOTNET_ROOT/tools
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# Check installed version
dotnet --version  # Must be 9.0+

# Install on Windows
winget install Microsoft.DotNet.SDK.9

# Install on Linux/macOS (via script)
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
./dotnet-install.ps1 -Channel 9.0

# Add to PATH (if needed) - add to $PROFILE
$env:DOTNET_ROOT = "$HOME\.dotnet"
$env:PATH = "$env:PATH;$env:DOTNET_ROOT;$env:DOTNET_ROOT\tools"
```
</details>

### Required Tools

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# Git (for version control)
git --version  # Any recent version

# Verify .NET analyzers work
dotnet build --no-incremental

# Run tests
dotnet test
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# Git (for version control)
git --version  # Any recent version

# Verify .NET analyzers work
dotnet build --no-incremental

# Run tests
dotnet test
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# Git (for version control)
git --version  # Any recent version

# Verify .NET analyzers work
dotnet build --no-incremental

# Run tests
dotnet test
```
</details>

---

## VS Code

### Required Extensions

| Extension | ID | Purpose |
|-----------|-----|---------|
| **C# Dev Kit** | `ms-dotnettools.csdevkit` | IntelliSense, debugging |
| **C#** | `ms-dotnettools.csharp` | Syntax, refactoring |
| **.NET Install Tool** | `ms-dotnettools.vscode-dotnet-runtime` | SDK management |
| **EditorConfig** | `editorconfig.editorconfig` | Code style |

**Install via terminal:**
```bash
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
code --install-extension editorconfig.editorconfig
```

### Recommended Extensions

| Extension | ID | Purpose |
|-----------|-----|---------|
| Error Lens | `usernamehw.errorlens` | Inline error display |
| GitLens | `eamodio.gitlens` | Git blame/history |
| Todo Tree | `gruntfuggly.todo-tree` | TODO tracking |
| XML | `redhat.vscode-xml` | XML config editing |

### Workspace Settings

Create `.vscode/settings.json`:
```json
{
  "dotnet.defaultSolution": "Jellyfin.Plugin.Lastfm.sln",
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "editor.formatOnSave": true,
  "editor.rulers": [120],
  "[csharp]": {
    "editor.defaultFormatter": "ms-dotnettools.csharp"
  },
  "files.exclude": {
    "**/bin": true,
    "**/obj": true
  }
}
```

### Launch Configuration

Create `.vscode/launch.json` for debugging:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Build Plugin",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "dotnet",
      "args": ["build", "${workspaceFolder}/Jellyfin.Plugin.Lastfm"],
      "cwd": "${workspaceFolder}",
      "console": "internalConsole"
    }
  ]
}
```

### Tasks Configuration

Create `.vscode/tasks.json`:
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build", "${workspaceFolder}/Jellyfin.Plugin.Lastfm", "-c", "Release"],
      "problemMatcher": "$msCompile",
      "group": { "kind": "build", "isDefault": true }
    },
    {
      "label": "clean",
      "command": "dotnet",
      "type": "process",
      "args": ["clean", "${workspaceFolder}/Jellyfin.Plugin.Lastfm"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "restore",
      "command": "dotnet",
      "type": "process",
      "args": ["restore", "${workspaceFolder}/Jellyfin.Plugin.Lastfm"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

---

## Zed

### Setup

1. **Install Zed**: https://zed.dev/download
2. **Install C# extension**: `Cmd/Ctrl+Shift+X` → Search "C#"
3. **Open project**: `zed /path/to/jellyfin-plugin-lastfm`

### Required Extensions

| Extension | Purpose |
|-----------|---------|
| **C#** | Language support via OmniSharp |
| **EditorConfig** | Code style consistency |

### Configuration

Edit Zed settings (`Cmd/Ctrl+,`):
```json
{
  "languages": {
    "C#": {
      "tab_size": 4,
      "hard_tabs": false,
      "format_on_save": "on"
    }
  },
  "lsp": {
    "omnisharp": {
      "binary": {
        "path": "~/.dotnet/tools/omnisharp"
      }
    }
  },
  "file_scan_exclusions": [
    "**/bin/**",
    "**/obj/**",
    "**/node_modules/**"
  ]
}
```

### OmniSharp Setup (for IntelliSense)

```bash
# Install OmniSharp globally
dotnet tool install -g omnisharp

# Verify installation
omnisharp --version
```

### Terminal Tasks in Zed

Use Zed's terminal (`Ctrl+`` `) for:

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# Build
dotnet build Jellyfin.Plugin.Lastfm -c Release

# Clean rebuild
dotnet clean && dotnet build Jellyfin.Plugin.Lastfm

# Run tests
dotnet test
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# Build
dotnet build Jellyfin.Plugin.Lastfm -c Release

# Clean rebuild
dotnet clean; and dotnet build Jellyfin.Plugin.Lastfm

# Run tests
dotnet test
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# Build
dotnet build Jellyfin.Plugin.Lastfm -c Release

# Clean rebuild
dotnet clean; dotnet build Jellyfin.Plugin.Lastfm

# Run tests
dotnet test
```
</details>

### Known Limitations

- Debugging requires external debugger (VS Code or command-line)
- Some refactoring features may be limited compared to VS Code
- Consider using VS Code for complex debugging sessions

---

## JetBrains Rider

### Setup

1. **Install Rider**: https://www.jetbrains.com/rider/
2. **Open solution**: `File → Open → Jellyfin.Plugin.Lastfm.sln`
3. Rider auto-detects .NET SDK

### Recommended Plugins

| Plugin | Purpose |
|--------|---------|
| .NET Core User Secrets | Secure config |
| EditorConfig | Code style |
| Key Promoter X | Learn shortcuts |

### Settings

**Editor → Code Style → C#:**
- Enable EditorConfig support
- Set "Reformat code on save"

**Build, Execution, Deployment → Toolset:**
- .NET CLI: Verify SDK path (auto-detected)

### Run Configuration

Create `Run/Debug Configuration`:
- **Type**: .NET Project
- **Project**: `Jellyfin.Plugin.Lastfm`
- **Configuration**: Release

### Rider Advantages

- Best-in-class refactoring
- Built-in decompiler
- Superior debugging experience
- Database tools (for future use)

---

## Neovim / Vim

### LSP Setup (nvim-lspconfig)

```lua
-- In init.lua
require('lspconfig').omnisharp.setup({
  cmd = { "omnisharp", "--languageserver", "--hostPID", tostring(vim.fn.getpid()) },
  root_dir = require('lspconfig.util').root_pattern("*.sln", "*.csproj"),
  settings = {
    FormattingOptions = {
      EnableEditorConfigSupport = true,
    },
    RoslynExtensionsOptions = {
      EnableAnalyzersSupport = true,
    },
  },
})
```

### Required

```bash
# OmniSharp language server
dotnet tool install -g omnisharp

# Neovim plugins (via your plugin manager)
# - nvim-lspconfig
# - nvim-cmp (completion)
# - mason.nvim (LSP installer)
```

---

## Common Issues & Solutions

### "SDK not found"

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# Check SDK installation
dotnet --list-sdks

# Ensure PATH includes dotnet
export PATH="$PATH:$HOME/.dotnet"
echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc  # or ~/.zshrc
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# Check SDK installation
dotnet --list-sdks

# Ensure PATH includes dotnet
fish_add_path $HOME/.dotnet

# Make permanent - add to ~/.config/fish/config.fish
echo 'fish_add_path $HOME/.dotnet' >> ~/.config/fish/config.fish
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# Check SDK installation
dotnet --list-sdks

# Ensure PATH includes dotnet (temporary)
$env:PATH = "$env:PATH;$HOME\.dotnet"

# Make permanent - add to $PROFILE
Add-Content $PROFILE '$env:PATH = "$env:PATH;$HOME\.dotnet"'
```
</details>

### "Analyzer errors in IDE but build succeeds"

<details>
<summary><b>All Shells</b></summary>

```bash
# Force analyzer reload (same in all shells)
dotnet build --no-incremental
```

**IDE-specific:**
- **VS Code**: `Cmd/Ctrl+Shift+P` → "OmniSharp: Restart"
- **Rider**: File → Invalidate Caches
- **Zed**: Restart the editor
</details>

### "IntelliSense not working"
1. Ensure `.sln` file exists and is loaded
2. Run `dotnet restore` in terminal
3. Restart language server

### "TreatWarningsAsErrors failing"
This project uses `TreatWarningsAsErrors=true`. Fix all warnings or add suppression with documented reason:
```xml
<NoWarn>CA1234</NoWarn>  <!-- Add comment explaining why -->
```

---

## VS Code Workspace Templates & Profiles

### Workspace Templates

VS Code doesn't have built-in project templates, but you can:

**1. Use `.code-workspace` files for project presets:**

Create `jellyfin-lastfm.code-workspace`:
```json
{
  "folders": [
    { "path": "." }
  ],
  "settings": {
    "dotnet.defaultSolution": "Jellyfin.Plugin.Lastfm.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "editor.formatOnSave": true
  },
  "extensions": {
    "recommendations": [
      "ms-dotnettools.csdevkit",
      "ms-dotnettools.csharp",
      "editorconfig.editorconfig"
    ]
  }
}
```

**2. Copy `.vscode` folder for new projects**

Create a template folder with preconfigured:
- `.vscode/settings.json`
- `.vscode/launch.json`
- `.vscode/tasks.json`
- `.vscode/extensions.json`

### VS Code Profiles

Profiles let you switch entire configurations (extensions, settings, keybindings):

**Create a Profile:**
1. `Cmd/Ctrl+Shift+P` → "Profiles: Create Profile"
2. Name it "Jellyfin Plugin Development"
3. Select which settings to include

**Switch Profiles:**
1. Click profile icon (bottom-left gear menu)
2. Select "Jellyfin Plugin Development"

**Export/Import Profiles:**
```bash
# Export profile to file
code --profile "Jellyfin Plugin Development" --export-profile profile.json

# Import profile
code --import-profile profile.json
```

**Recommended Profile Setup:**
- Extensions: C# Dev Kit, EditorConfig, Error Lens, GitLens
- Settings: Enable analyzers, format on save, rulers at 120
- Keybindings: Custom build shortcuts

### Multi-Branch Workspace Management

#### Option 1: Git Worktrees (Recommended)

Checkout multiple branches simultaneously in separate directories:

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# Create worktree for a feature branch
git worktree add ../lastfm-feature-branch feature/new-feature
git worktree add ../lastfm-bugfix bugfix/some-fix

# List all worktrees
git worktree list

# Remove worktree when done
git worktree remove ../lastfm-feature-branch
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# Create worktree for a feature branch
git worktree add ../lastfm-feature-branch feature/new-feature
git worktree add ../lastfm-bugfix bugfix/some-fix

# List all worktrees
git worktree list

# Remove worktree when done
git worktree remove ../lastfm-feature-branch
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# Create worktree for a feature branch
git worktree add ..\lastfm-feature-branch feature/new-feature
git worktree add ..\lastfm-bugfix bugfix/some-fix

# List all worktrees
git worktree list

# Remove worktree when done
git worktree remove ..\lastfm-feature-branch
```
</details>

#### Option 2: Multi-Root Workspace

Create a workspace file with multiple branch checkouts:

```json
{
  "folders": [
    { "name": "main", "path": "../jellyfin-plugin-lastfm-main" },
    { "name": "feature", "path": "../jellyfin-plugin-lastfm-feature" },
    { "name": "bugfix", "path": "../jellyfin-plugin-lastfm-bugfix" }
  ],
  "settings": {
    "git.autoRepositoryDetection": "subFolders"
  }
}
```

#### Option 3: Branch Switching with Stash

For quick context switching:

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# Stash current work
git stash push -m "WIP: feature work"

# Switch branch
git checkout other-branch

# Later, return and restore
git checkout original-branch
git stash pop
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# Stash current work
git stash push -m "WIP: feature work"

# Switch branch
git checkout other-branch

# Later, return and restore
git checkout original-branch
git stash pop
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# Stash current work
git stash push -m "WIP: feature work"

# Switch branch
git checkout other-branch

# Later, return and restore
git checkout original-branch
git stash pop
```
</details>

---

## Verification Checklist

After setup, verify everything works:

<details>
<summary><b>Bash / Zsh</b></summary>

```bash
# 1. Build succeeds with 0 warnings
dotnet build Jellyfin.Plugin.Lastfm -c Release

# 2. Analyzers run
dotnet build Jellyfin.Plugin.Lastfm --no-incremental 2>&1 | grep -E "warning|error"

# 3. Tests pass
dotnet test
```
</details>

<details>
<summary><b>Fish</b></summary>

```fish
# 1. Build succeeds with 0 warnings
dotnet build Jellyfin.Plugin.Lastfm -c Release

# 2. Analyzers run
dotnet build Jellyfin.Plugin.Lastfm --no-incremental 2>&1 | grep -E "warning|error"

# 3. Tests pass
dotnet test
```
</details>

<details>
<summary><b>PowerShell</b></summary>

```powershell
# 1. Build succeeds with 0 warnings
dotnet build Jellyfin.Plugin.Lastfm -c Release

# 2. Analyzers run
dotnet build Jellyfin.Plugin.Lastfm --no-incremental 2>&1 | Select-String "warning|error"

# 3. Tests pass
dotnet test
```
</details>

**IDE Verification:**
- [ ] Syntax highlighting works
- [ ] IntelliSense provides completions
- [ ] Go to Definition (`F12` / `Cmd+Click`) works
- [ ] Build task runs successfully
- [ ] Analyzer warnings appear inline

---

## Recommended Workflow

1. **Open project** via `.sln` file (not folder)
2. **Build first** to restore packages: `dotnet build`
3. **Enable analyzers** in IDE settings
4. **Format on save** to maintain consistency
5. **Use terminal** for complex build tasks

**Related**:
- [development-workflow.md](development-workflow.md) - Build & release process
- [csharp-patterns.md](csharp-patterns.md) - Code patterns to follow
````
