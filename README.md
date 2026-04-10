## jellyfin-plugin-lastfm

[![Build](https://github.com/lusoris/jellyfin-plugin-lastfm/actions/workflows/build-plugin.yaml/badge.svg)](https://github.com/lusoris/jellyfin-plugin-lastfm/actions/workflows/build-plugin.yaml)
[![Release](https://img.shields.io/github/v/release/lusoris/jellyfin-plugin-lastfm)](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Jellyfin](https://img.shields.io/badge/Jellyfin-10.11.8-00A4DC)](https://jellyfin.org/)
[![GitHub Stars](https://img.shields.io/github/stars/lusoris/jellyfin-plugin-lastfm)](https://github.com/lusoris/jellyfin-plugin-lastfm/stargazers)

> **Fork Notice**: This is a maintained fork of [jesseward/jellyfin-plugin-lastfm](https://github.com/jesseward/jellyfin-plugin-lastfm), which was archived on 2026-01-31.

Enables audio scrobbling to Last.FM as well as a metadata fetcher source.

This plug-in was migrated from the original Emby repository and has been adapted to function within the Jellyfin ecosystem. This plugin *cannot* be distributed with Jellyfin due to a missing compatible license.

## 🔧 Installation and Configuration

### Option 1: Add as Plugin Repository (Recommended)
Add this repository URL to Jellyfin's Plugin Repositories:

**Repository URL**: `https://raw.githubusercontent.com/lusoris/jellyfin-plugin-lastfm/main/manifest.json`

Steps:
1. Open Jellyfin Dashboard → Admin → Plugins → Repositories
2. Click "+" to add new repository
3. Paste the URL above
4. Click "Save"
5. Go to "Catalog" tab, find "Last.fm", and click "Install"

### Option 2: Manual Installation
1. Download the ZIP from the [latest release](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/latest)
2. Extract `Jellyfin.Plugin.Lastfm.dll` from the ZIP
3. Copy the DLL to your Jellyfin plugins directory:
   - **Linux**: `/var/lib/jellyfin/plugins/Lastfm/`
   - **Windows (Tray)**: `%ProgramData%\Jellyfin\Server\plugins\Lastfm\`
   - **Windows (Direct)**: `%UserProfile%\AppData\Local\jellyfin\plugins\Lastfm\`
   - **Docker**: Mount or copy to `/config/plugins/Lastfm/` inside the container
4. Restart Jellyfin

## ⚠️ License

This plugin has no explicit open-source license. It was originally adapted from Emby code and cannot be distributed with Jellyfin due to licensing incompatibilities. Use at your own discretion.

## 🙏 Credits

- Original plugin by [jesseward](https://github.com/jesseward)
- Adapted from the Emby Last.fm plugin
