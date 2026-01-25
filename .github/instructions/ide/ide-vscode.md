# VS Code Setup

Complete configuration for Visual Studio Code.

## Required Extensions

| Extension | ID | Purpose |
|-----------|-----|---------|
| **C# Dev Kit** | `ms-dotnettools.csdevkit` | IntelliSense, debugging |
| **C#** | `ms-dotnettools.csharp` | Syntax, refactoring |
| **.NET Install Tool** | `ms-dotnettools.vscode-dotnet-runtime` | SDK management |
| **EditorConfig** | `editorconfig.editorconfig` | Code style |

## Recommended Extensions

| Extension | ID | Purpose |
|-----------|-----|---------|
| Error Lens | `usernamehw.errorlens` | Inline error display |
| GitLens | `eamodio.gitlens` | Git blame/history |
| Todo Tree | `gruntfuggly.todo-tree` | TODO tracking |
| XML | `redhat.vscode-xml` | XML config editing |

## Workspace Settings

`.vscode/settings.json`:
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

## Launch Configuration

`.vscode/launch.json`:
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

## Tasks Configuration

`.vscode/tasks.json`:
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
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": ["test", "${workspaceFolder}"],
      "problemMatcher": "$msCompile",
      "group": { "kind": "test", "isDefault": true }
    },
    {
      "label": "clean",
      "command": "dotnet",
      "type": "process",
      "args": ["clean", "${workspaceFolder}/Jellyfin.Plugin.Lastfm"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

## Extensions Recommendations

`.vscode/extensions.json`:
```json
{
  "recommendations": [
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "editorconfig.editorconfig",
    "usernamehw.errorlens",
    "eamodio.gitlens"
  ]
}
```

---

## VS Code Profiles

Profiles let you switch entire configurations.

### Create Profile

1. `Cmd/Ctrl+Shift+P` → "Profiles: Create Profile"
2. Name: "Jellyfin Plugin Development"
3. Select settings to include

### Export/Import

```bash
# Export
code --profile "Jellyfin Plugin Development" --export-profile profile.json

# Import
code --import-profile profile.json
```

---

## Multi-Root Workspace

For working with multiple branches:

`jellyfin-multi.code-workspace`:
```json
{
  "folders": [
    { "name": "main", "path": "../jellyfin-plugin-lastfm-main" },
    { "name": "feature", "path": "../jellyfin-plugin-lastfm-feature" }
  ],
  "settings": {
    "git.autoRepositoryDetection": "subFolders"
  }
}
```

---

**Related:** [shell-config.md](shell-config.md) | [ide-zed.md](ide-zed.md) | [ide-rider.md](ide-rider.md)
