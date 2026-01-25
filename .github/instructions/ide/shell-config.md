# Shell Configuration Reference

Environment setup for different shells.

## PATH Configuration

### Bash (~/.bashrc or ~/.bash_profile)

```bash
# .NET SDK
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"

# Disable telemetry (optional)
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

### Zsh (~/.zshrc)

```zsh
# .NET SDK
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"

# Disable telemetry (optional)
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

### Fish (~/.config/fish/config.fish)

```fish
# .NET SDK
set -gx DOTNET_ROOT $HOME/.dotnet
fish_add_path $DOTNET_ROOT $DOTNET_ROOT/tools

# Disable telemetry (optional)
set -gx DOTNET_CLI_TELEMETRY_OPTOUT 1
```

### PowerShell ($PROFILE)

```powershell
# .NET SDK
$env:DOTNET_ROOT = "$HOME\.dotnet"
$env:PATH = "$env:PATH;$env:DOTNET_ROOT;$env:DOTNET_ROOT\tools"

# Disable telemetry (optional)
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
```

### Nushell (~/.config/nushell/env.nu)

```nu
$env.DOTNET_ROOT = $"($env.HOME)/.dotnet"
$env.PATH = ($env.PATH | split row (char esep) | append $env.DOTNET_ROOT | append $"($env.DOTNET_ROOT)/tools")
$env.DOTNET_CLI_TELEMETRY_OPTOUT = 1
```

---

## Common Commands by Shell

### Build Project

| Shell | Command |
|-------|---------|
| Bash/Zsh | `dotnet build -c Release && echo "Done"` |
| Fish | `dotnet build -c Release; and echo "Done"` |
| PowerShell | `dotnet build -c Release; Write-Host "Done"` |
| Nushell | `dotnet build -c Release; print "Done"` |

### Clean and Rebuild

| Shell | Command |
|-------|---------|
| Bash/Zsh | `dotnet clean && dotnet build` |
| Fish | `dotnet clean; and dotnet build` |
| PowerShell | `dotnet clean; dotnet build` |

### Run Tests with Coverage

| Shell | Command |
|-------|---------|
| Bash/Zsh | `dotnet test --collect:"XPlat Code Coverage" 2>&1 \| tee test.log` |
| Fish | `dotnet test --collect:"XPlat Code Coverage" 2>&1 \| tee test.log` |
| PowerShell | `dotnet test --collect:"XPlat Code Coverage" \| Tee-Object -FilePath test.log` |

### Find Warnings in Build Output

| Shell | Command |
|-------|---------|
| Bash/Zsh | `dotnet build 2>&1 \| grep -E "warning\|error"` |
| Fish | `dotnet build 2>&1 \| grep -E "warning\|error"` |
| PowerShell | `dotnet build 2>&1 \| Select-String "warning\|error"` |

---

## Git Worktrees (Multi-Branch)

### Create Worktree

| Shell | Command |
|-------|---------|
| Bash/Zsh/Fish | `git worktree add ../branch-name branch-name` |
| PowerShell | `git worktree add ..\branch-name branch-name` |

### List Worktrees

```bash
# All shells
git worktree list
```

### Remove Worktree

| Shell | Command |
|-------|---------|
| Bash/Zsh/Fish | `git worktree remove ../branch-name` |
| PowerShell | `git worktree remove ..\branch-name` |

---

## Aliases (Optional)

### Bash/Zsh

```bash
alias db='dotnet build -c Release'
alias dt='dotnet test'
alias dc='dotnet clean'
alias dr='dotnet restore'
```

### Fish

```fish
abbr -a db 'dotnet build -c Release'
abbr -a dt 'dotnet test'
abbr -a dc 'dotnet clean'
abbr -a dr 'dotnet restore'
```

### PowerShell

```powershell
Set-Alias db { dotnet build -c Release }
function dt { dotnet test $args }
function dc { dotnet clean $args }
function dr { dotnet restore $args }
```

---

**Related:** [ide/package-managers.md](ide/package-managers.md) | [workflow/development-workflow.md](workflow/development-workflow.md)
