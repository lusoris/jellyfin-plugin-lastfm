// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from album.getInfo API call.
/// </summary>
public class AlbumInfoResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the album info.
    /// </summary>
    [JsonPropertyName("album")]
    public AlbumInfo? Album { get; set; }
}

/// <summary>
/// Album information from Last.fm.
/// </summary>
public class AlbumInfo
{
    /// <summary>
    /// Gets or sets the album name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }

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

    /// <summary>
    /// Gets or sets the images.
    /// </summary>
    [JsonPropertyName("image")]
    public List<LastfmImage>? Images { get; set; }

    /// <summary>
    /// Gets or sets the listener count.
    /// </summary>
    [JsonPropertyName("listeners")]
    public string? Listeners { get; set; }

    /// <summary>
    /// Gets or sets the play count.
    /// </summary>
    [JsonPropertyName("playcount")]
    public string? Playcount { get; set; }

    /// <summary>
    /// Gets or sets the tracks.
    /// </summary>
    [JsonPropertyName("tracks")]
    public AlbumTracksInfo? Tracks { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public TagsInfo? Tags { get; set; }

    /// <summary>
    /// Gets or sets the wiki info.
    /// </summary>
    [JsonPropertyName("wiki")]
    public AlbumWiki? Wiki { get; set; }
}

/// <summary>
/// Album tracks container.
/// </summary>
public class AlbumTracksInfo
{
    /// <summary>
    /// Gets or sets the list of tracks.
    /// </summary>
    [JsonPropertyName("track")]
    public List<AlbumTrackInfo>? TrackList { get; set; }
}

/// <summary>
/// A track in an album.
/// </summary>
public class AlbumTrackInfo
{
    /// <summary>
    /// Gets or sets the track name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration in seconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the track number.
    /// </summary>
    [JsonPropertyName("@attr")]
    public TrackAttr? Attr { get; set; }
}

/// <summary>
/// Track attributes.
/// </summary>
public class TrackAttr
{
    /// <summary>
    /// Gets or sets the track rank/number.
    /// </summary>
    [JsonPropertyName("rank")]
    public int Rank { get; set; }
}

/// <summary>
/// Album wiki information.
/// </summary>
public class AlbumWiki
{
    /// <summary>
    /// Gets or sets the published date.
    /// </summary>
    [JsonPropertyName("published")]
    public string? Published { get; set; }

    /// <summary>
    /// Gets or sets the summary.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the full content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
