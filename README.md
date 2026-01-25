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

### Option 2: Manual Installation
1. Download the ZIP from the [latest release](https://github.com/lusoris/jellyfin-plugin-lastfm/releases/latest)
2. Extract `Jellyfin.Plugin.Lastfm.dll` from the ZIP
3. Copy the DLL to your Jellyfin plugins directory:
   - **Linux**: `/var/lib/jellyfin/plugins/Lastfm/`
   - **Windows (Tray)**: `%ProgramData%\Jellyfin\Server\plugins\Lastfm\`
   - **Windows (Direct)**: `%UserProfile%\AppData\Local\jellyfin\plugins\Lastfm\`
   - **Docker**: Mount or copy to `/config/plugins/Lastfm/` inside the container
4. Restart Jellyfin

---

![plugins](https://github.com/lusoris/jellyfin-plugin-lastfm/assets/465993/9adf1434-0ba8-4182-b267-6ce34d5933a7)
