## jellyfin-plugin-lastfm

Enables audio scrobbling to Last.FM as well as a metadata fetcher source.

This plug-in was adapted to function within the Jellyfin ecosystem. This plugin *cannot* be distributed with Jellyfin due to a missing compatible license.

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

### Option 2: Manual Download (10.11.6.0)
Download directly from GitHub release:
- **GitHub Release**: [github.com/lusoris/jellyfin-plugin-lastfm/releases/tag/10.11.6.0](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/tag/10.11.6.0)
- Direct Download: [lastfm_10.11.6.0.zip](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/download/10.11.6.0/lastfm_10.11.6.0.zip)

Upload the ZIP file to Jellyfin's Plugins section in the admin dashboard.

### Version Info
Latest version targets **Jellyfin 10.11.6** with .NET 9.0

---

![plugins](https://github.com/lusoris/jellyfin-plugin-lastfm/assets/465993/9adf1434-0ba8-4182-b267-6ce34d5933a7)
