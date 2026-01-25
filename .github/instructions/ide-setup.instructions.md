---
applyTo: "*"
---

# IDE & Environment Setup Index

Quick reference for development environment configuration.

## Choose Your Setup

| Need | Document |
|------|----------|
| **Install packages** | → [ide/package-managers.md](ide/package-managers.md) |
| **Shell config** | → [ide/shell-config.md](ide/shell-config.md) |
| **VS Code** | → [ide/ide-vscode.md](ide/ide-vscode.md) |
| **Zed** | → [ide/ide-zed.md](ide/ide-zed.md) |
| **Rider** | → [ide/ide-rider.md](ide/ide-rider.md) |
| **Neovim** | → [ide/ide-neovim.md](ide/ide-neovim.md) |

## Quick Start

```bash
# 1. Install .NET 9.0 (see package-managers.md for your OS)
dotnet --version  # Verify 9.0+

# 2. Clone and build
git clone https://github.com/jesseward/jellyfin-plugin-lastfm
cd jellyfin-plugin-lastfm
dotnet build -c Release

# 3. Run tests
dotnet test
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| SDK not found | Check PATH in [shell-config.md](ide/shell-config.md) |
| IntelliSense broken | Run `dotnet restore`, restart LSP |
| Analyzer errors | `dotnet build --no-incremental` |

---

**Related:** [development-workflow.md](development-workflow.md) | [testing.instructions.md](testing.instructions.md)
