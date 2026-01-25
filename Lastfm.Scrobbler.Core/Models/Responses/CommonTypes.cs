// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Standard Last.fm image with size and URL.
/// </summary>
public class LastfmImage
{
    /// <summary>
    /// Gets or sets the image size (small, medium, large, extralarge, mega).
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    [JsonPropertyName("#text")]
    public string? Url { get; set; }
}

/// <summary>
/// Standard pagination attributes from Last.fm API responses.
/// </summary>
public class PaginationAttributes
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [JsonPropertyName("page")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the results per page.
    /// </summary>
    [JsonPropertyName("perPage")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int PerPage { get; set; }

    /// <summary>
    /// Gets or sets the total pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total results.
    /// </summary>
    [JsonPropertyName("total")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the username (when applicable).
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }

    /// <summary>
    /// Gets a value indicating whether this is the last page.
    /// </summary>
    [JsonIgnore]
    public bool IsLastPage => Page >= TotalPages;
}

/// <summary>
/// Base artist reference with name and optional MBID.
/// Used in various response types.
/// </summary>
public class LastfmArtistRef
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
/// Base track reference with name, artist and optional MBID.
/// Used in various response types.
/// </summary>
public class LastfmTrackRef
{
    /// <summary>
    /// Gets or sets the track name.
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
/// Tag/genre information.
/// </summary>
public class TagInfo
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tag URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Container for tags.
/// </summary>
public class TagsInfo
{
    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    [JsonPropertyName("tag")]
    public List<TagInfo>? Tags { get; set; }
}
