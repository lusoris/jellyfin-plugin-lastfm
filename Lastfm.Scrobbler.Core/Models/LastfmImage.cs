// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models;

using System.Text.Json.Serialization;

/// <summary>
/// A model representing an image from the Last.fm API.
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
