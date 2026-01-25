// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from artist.getSimilar API call.
/// </summary>
public class SimilarArtistsResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the similar artists container.
    /// </summary>
    [JsonPropertyName("similarartists")]
    public SimilarArtistsContainer? SimilarArtists { get; set; }
}

/// <summary>
/// Container for similar artists.
/// </summary>
public class SimilarArtistsContainer
{
    /// <summary>
    /// Gets or sets the list of similar artists.
    /// </summary>
    [JsonPropertyName("artist")]
    public List<SimilarArtist>? Artists { get; set; }

    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    [JsonPropertyName("@attr")]
    public SimilarArtistsAttr? Attr { get; set; }
}

/// <summary>
/// A similar artist.
/// </summary>
public class SimilarArtist
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
    /// Gets or sets the match score (0-1).
    /// </summary>
    [JsonPropertyName("match")]
    public string? Match { get; set; }

    /// <summary>
    /// Gets or sets the Last.fm URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets the match score as a decimal.
    /// </summary>
    public double MatchScore => double.TryParse(Match, out var score) ? score : 0;
}

/// <summary>
/// Attributes for similar artists response.
/// </summary>
public class SimilarArtistsAttr
{
    /// <summary>
    /// Gets or sets the source artist name.
    /// </summary>
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }
}
