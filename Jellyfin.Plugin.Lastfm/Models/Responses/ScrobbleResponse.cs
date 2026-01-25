// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from track.scrobble.
/// </summary>
public class ScrobbleResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the scrobbles data.
    /// </summary>
    [JsonPropertyName("scrobbles")]
    public ScrobblesData? Scrobbles { get; set; }
}

/// <summary>
/// Scrobbles container.
/// </summary>
public class ScrobblesData
{
    /// <summary>
    /// Gets or sets the scrobble attributes.
    /// </summary>
    [JsonPropertyName("@attr")]
    public ScrobbleAttributes? Attributes { get; set; }
}

/// <summary>
/// Scrobble result attributes.
/// </summary>
public class ScrobbleAttributes
{
    /// <summary>
    /// Gets or sets the number of accepted scrobbles.
    /// </summary>
    [JsonPropertyName("accepted")]
    public int Accepted { get; set; }

    /// <summary>
    /// Gets or sets the number of ignored scrobbles.
    /// </summary>
    [JsonPropertyName("ignored")]
    public int Ignored { get; set; }
}
