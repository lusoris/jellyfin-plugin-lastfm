// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// A model representing tag information from the Last.fm API.
/// </summary>
public class TagsInfo
{
    /// <summary>
    /// Gets or sets the list of tags.
    /// </summary>
    [JsonPropertyName("tag")]
    public List<Tag>? Tags { get; set; }
}

/// <summary>
/// A model representing a single tag.
/// </summary>
public class Tag
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the tag URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
