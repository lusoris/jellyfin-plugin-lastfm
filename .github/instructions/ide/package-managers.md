# Package Manager Reference

Cross-platform installation commands for all required tools.

## .NET SDK 9.0

| Manager | Command |
|---------|---------|
| **apt** (Debian/Ubuntu) | `sudo apt install dotnet-sdk-9.0` |
| **dnf** (Fedora/RHEL) | `sudo dnf install dotnet-sdk-9.0` |
| **pacman** (Arch) | `sudo pacman -S dotnet-sdk` |
| **zypper** (openSUSE) | `sudo zypper install dotnet-sdk-9.0` |
| **brew** (macOS/Linux) | `brew install dotnet@9` |
| **winget** (Windows) | `winget install Microsoft.DotNet.SDK.9` |
| **choco** (Windows) | `choco install dotnet-sdk --version=9.0` |
| **scoop** (Windows) | `scoop install dotnet-sdk` |
| **nix** | `nix-env -iA nixpkgs.dotnet-sdk_9` |
| **asdf** | `asdf plugin add dotnet && asdf install dotnet 9.0.0` |

### Manual Install (all platforms)

```bash
# Official install script
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0

# PowerShell variant
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
./dotnet-install.ps1 -Channel 9.0
```

## Git

| Manager | Command |
|---------|---------|
| **apt** | `sudo apt install git` |
| **dnf** | `sudo dnf install git` |
| **pacman** | `sudo pacman -S git` |
| **zypper** | `sudo zypper install git` |
| **brew** | `brew install git` |
| **winget** | `winget install Git.Git` |
| **choco** | `choco install git` |
| **scoop** | `scoop install git` |

## OmniSharp (LSP for C#)

```bash
# All platforms via .NET tool
dotnet tool install -g omnisharp

# Verify
omnisharp --version
```

## VS Code Extensions (CLI)

```bash
# Required
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
code --install-extension editorconfig.editorconfig

# Recommended
code --install-extension usernamehw.errorlens
code --install-extension eamodio.gitlens
code --install-extension gruntfuggly.todo-tree
```

## Coverage Tools

```bash
# ReportGenerator (HTML coverage reports)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Coverlet (already in test project, but global option)
dotnet tool install -g coverlet.console
```

## Docker (for containerized testing)

| Manager | Command |
|---------|---------|
| **apt** | `sudo apt install docker.io` |
| **dnf** | `sudo dnf install docker` |
| **pacman** | `sudo pacman -S docker` |
| **brew** | `brew install --cask docker` |
| **winget** | `winget install Docker.DockerDesktop` |
| **choco** | `choco install docker-desktop` |

## Quick Setup Scripts

### Linux (auto-detect distro)

```bash
#!/bin/bash
if command -v apt &> /dev/null; then
    sudo apt update && sudo apt install -y dotnet-sdk-9.0 git
elif command -v dnf &> /dev/null; then
    sudo dnf install -y dotnet-sdk-9.0 git
elif command -v pacman &> /dev/null; then
    sudo pacman -Sy --noconfirm dotnet-sdk git
elif command -v zypper &> /dev/null; then
    sudo zypper install -y dotnet-sdk-9.0 git
else
    echo "Unknown package manager, using .NET install script"
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
fi
```

### macOS

```bash
brew install dotnet@9 git
```

### Windows (PowerShell Admin)

```powershell
winget install Microsoft.DotNet.SDK.9 Git.Git
```

---

**Related:** [shell-config.md](shell-config.md) | [ide-vscode.md](ide-vscode.md)
