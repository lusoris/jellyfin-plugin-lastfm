// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from tag.getTopTracks API call.
/// </summary>
public class TagTopTracksResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the tracks data.
    /// </summary>
    [JsonPropertyName("tracks")]
    public TagTracksData? Tracks { get; set; }
}

/// <summary>
/// Container for tag top tracks.
/// </summary>
public class TagTracksData
{
    /// <summary>
    /// Gets or sets the list of tracks.
    /// </summary>
    [JsonPropertyName("track")]
    public List<TagTrack>? TrackList { get; set; }

    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    [JsonPropertyName("@attr")]
    public TagTopAttributes? Attributes { get; set; }
}

/// <summary>
/// Track in tag top tracks.
/// </summary>
public class TagTrack
{
    /// <summary>
    /// Gets or sets the track name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist info.
    /// </summary>
    [JsonPropertyName("artist")]
    public TagTrackArtist? Artist { get; set; }

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    /// <summary>
    /// Gets or sets the MusicBrainz ID.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? Mbid { get; set; }
}

/// <summary>
/// Artist info in tag track.
/// </summary>
public class TagTrackArtist
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
    /// Gets or sets the URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Attributes for tag top results.
/// </summary>
public class TagTopAttributes
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the current page.
    /// </summary>
    [JsonPropertyName("page")]
    public string? Page { get; set; }

    /// <summary>
    /// Gets or sets the total pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public string? TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total results.
    /// </summary>
    [JsonPropertyName("total")]
    public string? Total { get; set; }
}
