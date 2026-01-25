// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from artist.getInfo API call.
/// </summary>
public class ArtistInfoResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the artist info.
    /// </summary>
    [JsonPropertyName("artist")]
    public ArtistInfo? Artist { get; set; }
}

/// <summary>
/// Artist information from Last.fm.
/// </summary>
public class ArtistInfo
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

    /// <summary>
    /// Gets or sets the images.
    /// </summary>
    [JsonPropertyName("image")]
    public List<LastfmImage>? Images { get; set; }

    /// <summary>
    /// Gets or sets the bio.
    /// </summary>
    [JsonPropertyName("bio")]
    public ArtistBio? Bio { get; set; }

    /// <summary>
    /// Gets or sets the stats.
    /// </summary>
    [JsonPropertyName("stats")]
    public ArtistStats? Stats { get; set; }

    /// <summary>
    /// Gets or sets the similar artists.
    /// </summary>
    [JsonPropertyName("similar")]
    public SimilarArtistsInfo? Similar { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public TagsInfo? Tags { get; set; }
}

/// <summary>
/// Artist biography.
/// </summary>
public class ArtistBio
{
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

/// <summary>
/// Artist statistics.
/// </summary>
public class ArtistStats
{
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
}

/// <summary>
/// Similar artists container.
/// </summary>
public class SimilarArtistsInfo
{
    /// <summary>
    /// Gets or sets the list of similar artists.
    /// </summary>
    [JsonPropertyName("artist")]
    public List<SimilarArtistInfo>? Artists { get; set; }
}

/// <summary>
/// A similar artist entry.
/// </summary>
public class SimilarArtistInfo
{
    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Tags container.
/// </summary>
public class TagsInfo
{
    /// <summary>
    /// Gets or sets the list of tags.
    /// </summary>
    [JsonPropertyName("tag")]
    public List<TagInfo>? Tags { get; set; }
}

/// <summary>
/// A tag entry.
/// </summary>
public class TagInfo
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Last.fm image entry.
/// </summary>
public class LastfmImage
{
    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    [JsonPropertyName("#text")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the image size.
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }
}
