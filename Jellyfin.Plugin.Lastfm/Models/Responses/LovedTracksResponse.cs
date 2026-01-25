// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from user.getLovedTracks.
/// </summary>
public class LovedTracksResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the loved tracks container.
    /// </summary>
    [JsonPropertyName("lovedtracks")]
    public LovedTracksContainer? LovedTracks { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are any loved tracks.
    /// </summary>
    [JsonIgnore]
    public bool HasTracks => LovedTracks?.Tracks?.Count > 0;
}

/// <summary>
/// Container for loved tracks.
/// </summary>
public class LovedTracksContainer
{
    /// <summary>
    /// Gets or sets the list of tracks.
    /// </summary>
    [JsonPropertyName("track")]
    public List<LovedTrack>? Tracks { get; set; }

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    [JsonPropertyName("@attr")]
    public PaginationAttributes? Attributes { get; set; }
}

/// <summary>
/// A loved track from Last.fm.
/// </summary>
public class LovedTrack
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
    public string? MusicBrainzId { get; set; }

    /// <summary>
    /// Gets or sets the artist information.
    /// </summary>
    [JsonPropertyName("artist")]
    public LovedTrackArtist? Artist { get; set; }
}

/// <summary>
/// Artist information for a loved track.
/// </summary>
public class LovedTrackArtist
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

/// <summary>
/// Pagination attributes for paginated responses.
/// </summary>
public class PaginationAttributes
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets a value indicating whether this is the last page.
    /// </summary>
    [JsonIgnore]
    public bool IsLastPage => Page >= TotalPages;
}
