---
applyTo: "**/*.cs"
---

# Last.fm API Reference

This instruction file contains the complete Last.fm API structure for the Jellyfin Last.fm Plugin.

## API Base URL

```
https://ws.audioscrobbler.com/2.0/
```

All requests use `format=json` parameter.

## Authentication

### auth.getMobileSession
Get a session key for a user (mobile auth flow).

**Method:** `auth.getMobileSession` (POST, requires signature)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| username | Yes | Last.fm username |
| password | Yes | Last.fm password |
| api_key | Yes | Your API key |
| api_sig | Yes | MD5 signature |

**Response:**
```json
{
  "session": {
    "name": "username",
    "key": "SESSION_KEY",
    "subscriber": 0
  }
}
```

## Scrobbling Methods

### track.scrobble
Scrobble a track (mark as played).

**Method:** `track.scrobble` (POST, requires signature + session)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| artist | Yes | Artist name |
| track | Yes | Track name |
| timestamp | Yes | Unix timestamp when track started playing |
| album | No | Album name |
| albumArtist | No | Album artist (if different from track artist) |
| mbid | No | MusicBrainz Track ID |
| duration | No | Track length in seconds |
| api_key | Yes | Your API key |
| api_sig | Yes | MD5 signature |
| sk | Yes | Session key |

**Scrobble Rules (from Last.fm docs):**
- Track must be longer than 30 seconds
- Track must have played for at least half its duration OR 4 minutes (whichever is earlier)

**Response:**
```json
{
  "scrobbles": {
    "@attr": { "accepted": 1, "ignored": 0 },
    "scrobble": {
      "artist": { "corrected": "0", "#text": "Artist Name" },
      "album": { "corrected": "0", "#text": "Album Name" },
      "track": { "corrected": "0", "#text": "Track Name" },
      "timestamp": "1234567890"
    }
  }
}
```

### track.updateNowPlaying
Update "Now Playing" status.

**Method:** `track.updateNowPlaying` (POST, requires signature + session)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| artist | Yes | Artist name |
| track | Yes | Track name |
| album | No | Album name |
| albumArtist | No | Album artist |
| mbid | No | MusicBrainz Track ID |
| duration | No | Track length in seconds |
| api_key | Yes | Your API key |
| api_sig | Yes | MD5 signature |
| sk | Yes | Session key |

## Love/Unlove Methods

### track.love
Mark a track as loved.

**Method:** `track.love` (POST, requires signature + session)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| artist | Yes | Artist name |
| track | Yes | Track name |
| api_key | Yes | Your API key |
| api_sig | Yes | MD5 signature |
| sk | Yes | Session key |

### track.unlove
Remove loved status from a track.

**Method:** `track.unlove` (POST, requires signature + session)

**Parameters:** Same as track.love

## User Data Methods (Read-Only)

### user.getLovedTracks
Get loved tracks for a user.

**Method:** `user.getLovedTracks` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| user | Yes | Last.fm username |
| limit | No | Results per page (default 50, max 1000) |
| page | No | Page number |
| api_key | Yes | Your API key |

**Response:**
```json
{
  "lovedtracks": {
    "@attr": { "page": "1", "perPage": "50", "totalPages": "10", "total": "500" },
    "track": [
      {
        "name": "Track Name",
        "mbid": "musicbrainz-id",
        "url": "https://www.last.fm/...",
        "date": { "uts": "1234567890", "#text": "01 Jan 2024, 12:00" },
        "artist": {
          "name": "Artist Name",
          "mbid": "artist-mbid",
          "url": "https://www.last.fm/..."
        }
      }
    ]
  }
}
```

### user.getTopTracks
Get top tracks for a user (includes playcount).

**Method:** `user.getTopTracks` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| user | Yes | Last.fm username |
| period | No | overall, 7day, 1month, 3month, 6month, 12month |
| limit | No | Results per page |
| page | No | Page number |
| api_key | Yes | Your API key |

**Response includes `playcount` per track** - can sync to Jellyfin!

```json
{
  "toptracks": {
    "track": [
      {
        "name": "Track Name",
        "playcount": "42",
        "artist": { "name": "Artist Name", "mbid": "..." }
      }
    ]
  }
}
```

### user.getRecentTracks
Get recently played tracks.

**Method:** `user.getRecentTracks` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| user | Yes | Last.fm username |
| limit | No | Results per page (max 200) |
| page | No | Page number |
| from | No | Unix timestamp to start from |
| to | No | Unix timestamp to end at |
| extended | No | Include extended data (0/1) |
| api_key | Yes | Your API key |

### user.getInfo
Get user profile info.

**Method:** `user.getInfo` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| user | Yes | Last.fm username |
| api_key | Yes | Your API key |

## Metadata Methods

### track.getInfo
Get track metadata.

**Method:** `track.getInfo` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| artist | Yes* | Artist name |
| track | Yes* | Track name |
| mbid | Yes* | MusicBrainz Track ID (alternative to artist+track) |
| username | No | Include user's playcount/loved status |
| api_key | Yes | Your API key |

### artist.getInfo
Get artist metadata including images.

**Method:** `artist.getInfo` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| artist | Yes* | Artist name |
| mbid | Yes* | MusicBrainz Artist ID |
| username | No | Include user stats |
| api_key | Yes | Your API key |

### album.getInfo
Get album metadata including images.

**Method:** `album.getInfo` (GET)

**Parameters:**
| Parameter | Required | Description |
|-----------|----------|-------------|
| artist | Yes* | Artist name |
| album | Yes* | Album name |
| mbid | Yes* | MusicBrainz Release ID |
| username | No | Include user stats |
| api_key | Yes | Your API key |

## API Signature Generation

For authenticated requests, generate MD5 signature:

1. Sort all parameters alphabetically by key (excluding `format`)
2. Concatenate as `key1value1key2value2...`
3. Append shared secret
4. Calculate MD5 hash

**Example (C#):**
```csharp
public static string GenerateSignature(Dictionary<string, string> parameters, string secret)
{
    var sorted = parameters
        .Where(p => p.Key != "format")
        .OrderBy(p => p.Key)
        .Select(p => $"{p.Key}{p.Value}");
    
    var signatureBase = string.Join("", sorted) + secret;
    
    using var md5 = MD5.Create();
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
    return Convert.ToHexString(hash).ToLowerInvariant();
}
```

## Error Responses

```json
{
  "error": 6,
  "message": "User not found"
}
```

**Common Error Codes:**
| Code | Description |
|------|-------------|
| 2 | Invalid service |
| 3 | Invalid method |
| 4 | Authentication failed |
| 6 | Parameter missing/invalid |
| 8 | Operation failed |
| 9 | Invalid session key |
| 10 | Invalid API key |
| 11 | Service offline |
| 13 | Invalid signature |
| 14 | Unauthorized token |
| 26 | Suspended API key |
| 29 | Rate limit exceeded |

## Rate Limits

- No official rate limit documented
- Best practice: 1 request per second max
- Batch scrobbles supported (up to 50 per request)

## Plugin Features Matrix

| Feature | Last.fm API | Jellyfin API | Sync Direction |
|---------|-------------|--------------|----------------|
| Scrobble | track.scrobble | PlaybackStopped event | Jellyfin → Last.fm |
| Now Playing | track.updateNowPlaying | PlaybackStart event | Jellyfin → Last.fm |
| Love/Unlove | track.love/unlove | UserData.IsFavorite | Bidirectional |
| Play Count | user.getTopTracks | UserData.PlayCount | Last.fm → Jellyfin ✅ |
| Recent Plays | user.getRecentTracks | - | Read-only |

## Notes for Implementation

1. **MusicBrainz IDs** - Always include when available for better matching
2. **Album Artist** - Important for compilations/soundtracks
3. **Timestamps** - Unix timestamps in seconds (not milliseconds!)
4. **UTF-8 Encoding** - All strings must be UTF-8 encoded
5. **URL Encoding** - POST body uses `application/x-www-form-urlencoded`
