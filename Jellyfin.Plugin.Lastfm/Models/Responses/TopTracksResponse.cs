// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from user.getTopTracks.
/// </summary>
public class TopTracksResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the top tracks container.
    /// </summary>
    [JsonPropertyName("toptracks")]
    public TopTracksContainer? TopTracks { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are any tracks.
    /// </summary>
    [JsonIgnore]
    public bool HasTracks => TopTracks?.Tracks?.Count > 0;
}

/// <summary>
/// Container for top tracks.
/// </summary>
public class TopTracksContainer
{
    /// <summary>
    /// Gets or sets the list of tracks.
    /// </summary>
    [JsonPropertyName("track")]
    public List<TopTrack>? Tracks { get; set; }

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    [JsonPropertyName("@attr")]
    public PaginationAttributes? Attributes { get; set; }
}

/// <summary>
/// A top track from Last.fm with play count.
/// </summary>
public class TopTrack
{
    /// <summary>
    /// Gets or sets the track name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the play count.
    /// </summary>
    [JsonPropertyName("playcount")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int PlayCount { get; set; }

    /// <summary>
    /// Gets or sets the MusicBrainz ID.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? MusicBrainzId { get; set; }

    /// <summary>
    /// Gets or sets the artist information.
    /// </summary>
    [JsonPropertyName("artist")]
    public TopTrackArtist? Artist { get; set; }
}

/// <summary>
/// Artist information for a top track.
/// </summary>
public class TopTrackArtist
{
    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MusicBrainz ID.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? MusicBrainzId { get; set; }
}
