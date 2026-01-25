# Jellyfin Audio Entities

Music-related entity types and provider IDs.

## Audio (Track)

**Namespace:** `MediaBrowser.Controller.Entities.Audio`
**Kind:** `BaseItemKind.Audio`

```csharp
public class Audio : BaseItem, IHasAlbumArtist, IHasArtist
{
    public IReadOnlyList<string> Artists { get; set; }
    public IReadOnlyList<string> AlbumArtists { get; set; }
    public string Album { get; set; }
    public string Name { get; }           // Track title
    public long? RunTimeTicks { get; }    // Duration in ticks
    public int? IndexNumber { get; }      // Track number
    public Dictionary<string, string> ProviderIds { get; }
}
```

## MusicAlbum

**Kind:** `BaseItemKind.MusicAlbum`

```csharp
public class MusicAlbum : Folder
{
    public IReadOnlyList<string> AlbumArtists { get; set; }
    public IEnumerable<Audio> Tracks { get; }
    public string AlbumArtist { get; }
}
```

## MusicArtist

**Kind:** `BaseItemKind.MusicArtist`

```csharp
public class MusicArtist : Folder
{
    public string Name { get; set; }
    public string Overview { get; set; }
}
```

---

## Provider IDs (MusicBrainz)

**Namespace:** `MediaBrowser.Model.Entities`

| Provider | Enum Value | Description |
|----------|------------|-------------|
| `MusicBrainzRecording` | 19 | Recording MBID (preferred for tracks) |
| `MusicBrainzTrack` | 18 | Track MBID (release-specific) |
| `MusicBrainzAlbum` | 6 | Release MBID |
| `MusicBrainzReleaseGroup` | 11 | Release Group MBID |
| `MusicBrainzArtist` | 10 | Artist MBID |
| `MusicBrainzAlbumArtist` | 9 | Album Artist MBID |

### Usage

```csharp
// Get provider ID
audio.TryGetProviderId(MetadataProvider.MusicBrainzRecording, out var mbid);

// Set provider ID
audio.SetProviderId(MetadataProvider.MusicBrainzRecording, "12345");

// Direct access
var mbid = audio.ProviderIds.GetValueOrDefault("MusicBrainzRecording");
```

### Tag Mapping

| Audio File Tag | Jellyfin Provider |
|----------------|-------------------|
| MUSICBRAINZ_TRACKID | MusicBrainzRecording |
| MUSICBRAINZ_RELEASETRACKID | MusicBrainzTrack |
| MUSICBRAINZ_ALBUMID | MusicBrainzAlbum |
| MUSICBRAINZ_RELEASEGROUPID | MusicBrainzReleaseGroup |
| MUSICBRAINZ_ARTISTID | MusicBrainzArtist |

---

## Query Examples

### Find by MBID

```csharp
var query = new InternalItemsQuery
{
    IncludeItemTypes = [BaseItemKind.Audio],
    Recursive = true,
    Limit = 1,
    HasAnyProviderId = new Dictionary<string, string>
    {
        [MetadataProvider.MusicBrainzRecording.ToString()] = mbid
    }
};
var track = _libraryManager.GetItemList(query).OfType<Audio>().FirstOrDefault();
```

### Find by Artist + Track Name

```csharp
var query = new InternalItemsQuery
{
    IncludeItemTypes = [BaseItemKind.Audio],
    Recursive = true,
    SearchTerm = trackName
};
var track = _libraryManager.GetItemList(query)
    .OfType<Audio>()
    .FirstOrDefault(a => 
        a.Name.Equals(trackName, StringComparison.OrdinalIgnoreCase) &&
        a.Artists.Any(ar => ar.Equals(artistName, StringComparison.OrdinalIgnoreCase)));
```

### Get User Favorites

```csharp
var query = new InternalItemsQuery(user)
{
    IncludeItemTypes = [BaseItemKind.Audio],
    IsFavorite = true,
    Recursive = true,
    OrderBy = [(ItemSortBy.DatePlayed, SortOrder.Descending)]
};
```

---

**Related:** [api/jellyfin-userdata.md](api/jellyfin-userdata.md) | [api/jellyfin-interfaces.md](api/jellyfin-interfaces.md)
