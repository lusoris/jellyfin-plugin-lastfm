# Zed Setup

Configuration for Zed editor.

## Installation

| Platform | Command |
|----------|---------|
| macOS | `brew install --cask zed` |
| Linux | Download from https://zed.dev/download |
| Windows | Not yet supported |

## Required Extensions

| Extension | Purpose |
|-----------|---------|
| **C#** | Language support via OmniSharp |
| **EditorConfig** | Code style consistency |

Install via: `Cmd/Ctrl+Shift+X` → Search extension name

## Settings

Edit: `Cmd/Ctrl+,`

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

## OmniSharp Setup

Required for IntelliSense:

```bash
# Install globally
dotnet tool install -g omnisharp

# Verify
omnisharp --version
```

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Terminal | `` Ctrl+` `` |
| Command Palette | `Cmd/Ctrl+Shift+P` |
| Go to File | `Cmd/Ctrl+P` |
| Go to Symbol | `Cmd/Ctrl+Shift+O` |

## Known Limitations

- ⚠️ Debugging requires external debugger
- ⚠️ Some refactoring features limited vs VS Code
- ⚠️ Use VS Code for complex debugging sessions

---

**Related:** [ide/ide-vscode.md](ide/ide-vscode.md) | [ide/shell-config.md](ide/shell-config.md)
