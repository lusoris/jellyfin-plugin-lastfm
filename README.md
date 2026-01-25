# Jellyfin Last.fm Plugin

[![Build](https://github.com/lusoris/jellyfin-plugin-lastfm/actions/workflows/build-plugin.yaml/badge.svg)](https://github.com/lusoris/jellyfin-plugin-lastfm/actions/workflows/build-plugin.yaml)
[![Release](https://img.shields.io/github/v/release/lusoris/jellyfin-plugin-lastfm)](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Jellyfin](https://img.shields.io/badge/Jellyfin-10.11.6-00A4DC)](https://jellyfin.org/)
[![License](https://img.shields.io/badge/License-GPL--2.0-blue.svg)](LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/lusoris/jellyfin-plugin-lastfm)](https://github.com/lusoris/jellyfin-plugin-lastfm/stargazers)

> **Fork Notice**: This is a maintained fork of [jesseward/jellyfin-plugin-lastfm](https://github.com/jesseward/jellyfin-plugin-lastfm), which was archived on 2026-01-31.

Scrobble your Jellyfin music playback to [Last.fm](https://www.last.fm/), sync loved tracks, and fetch artist/album metadata.

## ✨ Features

### Core Features
- **Scrobbling** - Automatically track plays to Last.fm with offline queue support
- **Now Playing** - Show what you're listening to in real-time
- **Loved Tracks Sync** - Bidirectional sync between Jellyfin favorites and Last.fm loved tracks
- **Play Count Import** - Sync your Last.fm scrobble history to Jellyfin

### Smart Playlists
- **Similar Artists Mix** - Discover music from artists similar to your favorites
- **Similar Tracks Mix** - Find tracks similar to your loved songs
- **Rediscover Favorites** - Revisit loved tracks you haven't played recently

### Custom UI Pages
- **Recommendations Page** - Create smart playlists with one click
- **Statistics Page** - View connection status, enabled features, and queue info

### Developer Features
- **REST API** - Full API for authentication, status, and playlist creation
- **Batch Scrobbling** - Submit up to 50 scrobbles at once
- **Offline Queue** - Failed scrobbles are automatically retried

## 🔧 Installation

### Option 1: Plugin Repository (Recommended)

Add this repository URL to Jellyfin's Plugin Repositories:

```
https://raw.githubusercontent.com/lusoris/jellyfin-plugin-lastfm/main/manifest.json
```

**Steps:**
1. Open Jellyfin Dashboard → Admin → Plugins → Repositories
2. Click "+" to add new repository
3. Paste the URL above
4. Click "Save"
5. Go to "Catalog" tab, find "Last.fm", and click "Install"
6. Restart Jellyfin

### Option 2: Manual Installation

1. Download the ZIP from the [latest release](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/latest)
2. Extract `Jellyfin.Plugin.Lastfm.dll` from the ZIP
3. Copy the DLL to your Jellyfin plugins directory:

| Platform | Path |
|----------|------|
| Linux | `/var/lib/jellyfin/plugins/Lastfm/` |
| Windows (Tray) | `%ProgramData%\Jellyfin\Server\plugins\Lastfm\` |
| Windows (Direct) | `%UserProfile%\AppData\Local\jellyfin\plugins\Lastfm\` |
| Docker | `/config/plugins/Lastfm/` inside the container |
| macOS | `~/.local/share/jellyfin/plugins/Lastfm/` |

4. Restart Jellyfin

## ⚙️ Configuration

1. Go to **Dashboard** → **Plugins** → **Last.fm**
2. Enter your Last.fm **username** and **password**
3. Click **Save**
4. The plugin will automatically exchange your password for a permanent session key

> **Security Note**: Your password is only used once to obtain a session key and is never stored.

### Per-User Options

| Option | Description |
|--------|-------------|
| Enable Scrobbling | Track plays to Last.fm |
| Send Now Playing | Show currently playing track on Last.fm |
| Sync Favorites → Loved | Sync Jellyfin favorites to Last.fm loved tracks |
| Sync Unfavorites → Unloved | Remove loved status when unfavorited |
| Import Loved Tracks | Import Last.fm loved tracks as Jellyfin favorites |
| Import Play Counts | Sync Last.fm play counts to Jellyfin |

### Scheduled Tasks

The plugin adds these scheduled tasks (Dashboard → Scheduled Tasks):

| Task | Description | Default |
|------|-------------|---------|
| Import Loved Tracks | Syncs loved tracks from Last.fm | Daily |
| Sync Play Counts | Imports play counts from Last.fm | Weekly |
| Process Scrobble Queue | Submits queued offline scrobbles | Every 15 min |

### REST API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Lastfm/Authenticate` | POST | Authenticate user with Last.fm |
| `/Lastfm/Status/{userId}` | GET | Get user connection status |
| `/Lastfm/Disconnect/{userId}` | POST | Disconnect user from Last.fm |
| `/Lastfm/Queue/{userId}` | GET | Get pending scrobble queue |
| `/Lastfm/Playlists/SimilarArtists` | POST | Create similar artists playlist |
| `/Lastfm/Playlists/SimilarTracks` | POST | Create similar tracks playlist |
| `/Lastfm/Playlists/RediscoverFavorites` | POST | Create rediscover favorites playlist |

## 🏗️ Building from Source

```bash
# Clone the repository
git clone https://github.com/lusoris/jellyfin-plugin-lastfm.git
cd jellyfin-plugin-lastfm

# Build
dotnet build Jellyfin.Plugin.Lastfm/Jellyfin.Plugin.Lastfm.csproj -c Release

# The DLL will be in: Jellyfin.Plugin.Lastfm/bin/Release/net9.0/
```

## 📜 License

This project is licensed under the **GPL-2.0** License - see the [LICENSE](LICENSE) file for details.

## 🙏 Credits

- Original plugin concept from the Emby Last.fm plugin
- Maintained fork of [jesseward/jellyfin-plugin-lastfm](https://github.com/jesseward/jellyfin-plugin-lastfm)
- [Last.fm](https://www.last.fm/) for providing the API
- [Jellyfin](https://jellyfin.org/) team for the media server

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📋 Related Links

- [Jellyfin](https://jellyfin.org/)
- [Last.fm API Documentation](https://www.last.fm/api)
- [Plugin Roadmap](.github/instructions/ROADMAP.md)
