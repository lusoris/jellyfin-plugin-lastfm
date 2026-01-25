// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from track.getSimilar API call.
/// </summary>
public class SimilarTracksResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the similar tracks container.
    /// </summary>
    [JsonPropertyName("similartracks")]
    public SimilarTracksContainer? SimilarTracks { get; set; }
}

/// <summary>
/// Container for similar tracks.
/// </summary>
public class SimilarTracksContainer
{
    /// <summary>
    /// Gets or sets the list of similar tracks.
    /// </summary>
    [JsonPropertyName("track")]
    public List<SimilarTrack>? Tracks { get; set; }

    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    [JsonPropertyName("@attr")]
    public SimilarTracksAttr? Attr { get; set; }
}

/// <summary>
/// A similar track.
/// </summary>
public class SimilarTrack
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
    public long Playcount { get; set; }

    /// <summary>
    /// Gets or sets the MusicBrainz ID.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? Mbid { get; set; }

    /// <summary>
    /// Gets or sets the match score (0-1).
    /// </summary>
    [JsonPropertyName("match")]
    public double Match { get; set; }

    /// <summary>
    /// Gets or sets the artist info.
    /// </summary>
    [JsonPropertyName("artist")]
    public SimilarTrackArtist? Artist { get; set; }

    /// <summary>
    /// Gets or sets the duration in seconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the Last.fm URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Artist info for a similar track.
/// </summary>
public class SimilarTrackArtist
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
    public string? Mbid { get; set; }

    /// <summary>
    /// Gets or sets the Last.fm URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Attributes for similar tracks response.
/// </summary>
public class SimilarTracksAttr
{
    /// <summary>
    /// Gets or sets the source artist name.
    /// </summary>
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

    /// <summary>
    /// Gets or sets the source track name.
    /// </summary>
    [JsonPropertyName("track")]
    public string? Track { get; set; }
}
