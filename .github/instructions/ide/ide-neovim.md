# Neovim / Vim Setup

Configuration for Neovim with LSP.

## Prerequisites

```bash
# Neovim 0.9+
nvim --version

# OmniSharp LSP
dotnet tool install -g omnisharp
```

## LSP Configuration

`~/.config/nvim/init.lua`:

```lua
-- OmniSharp setup
require('lspconfig').omnisharp.setup({
  cmd = { 
    "omnisharp", 
    "--languageserver", 
    "--hostPID", tostring(vim.fn.getpid()) 
  },
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

## Required Plugins

Using [lazy.nvim](https://github.com/folke/lazy.nvim):

```lua
{
  -- LSP
  'neovim/nvim-lspconfig',
  
  -- Completion
  'hrsh7th/nvim-cmp',
  'hrsh7th/cmp-nvim-lsp',
  
  -- LSP installer
  'williamboman/mason.nvim',
  'williamboman/mason-lspconfig.nvim',
  
  -- Treesitter (syntax)
  'nvim-treesitter/nvim-treesitter',
}
```

## Mason Setup (Alternative)

Using Mason to manage LSP servers:

```lua
require('mason').setup()
require('mason-lspconfig').setup({
  ensure_installed = { 'omnisharp' }
})
```

## Keybindings

```lua
-- LSP keymaps
vim.keymap.set('n', 'gd', vim.lsp.buf.definition, { desc = 'Go to definition' })
vim.keymap.set('n', 'gr', vim.lsp.buf.references, { desc = 'Find references' })
vim.keymap.set('n', 'K', vim.lsp.buf.hover, { desc = 'Hover docs' })
vim.keymap.set('n', '<leader>rn', vim.lsp.buf.rename, { desc = 'Rename' })
vim.keymap.set('n', '<leader>ca', vim.lsp.buf.code_action, { desc = 'Code action' })
vim.keymap.set('n', '<leader>f', vim.lsp.buf.format, { desc = 'Format' })
```

## Build Commands

```vim
" Build
:!dotnet build -c Release

" Test
:!dotnet test

" Or use vim-dispatch
:Dispatch dotnet build
```

## Minimal Config

For quick setup:

```lua
-- init.lua minimal
require('lspconfig').omnisharp.setup({})
```

---

**Related:** [ide/ide-vscode.md](ide/ide-vscode.md) | [ide/shell-config.md](ide/shell-config.md)
