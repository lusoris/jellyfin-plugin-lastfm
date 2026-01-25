// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from user.getTopTags API call.
/// </summary>
public class UserTopTagsResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the top tags data.
    /// </summary>
    [JsonPropertyName("toptags")]
    public TopTagsData? TopTags { get; set; }
}

/// <summary>
/// Container for user's top tags.
/// </summary>
public class TopTagsData
{
    /// <summary>
    /// Gets or sets the list of tags.
    /// </summary>
    [JsonPropertyName("tag")]
    public List<UserTag>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the attributes.
    /// </summary>
    [JsonPropertyName("@attr")]
    public UserTopTagsAttributes? Attributes { get; set; }
}

/// <summary>
/// Tag in user's top tags.
/// </summary>
public class UserTag
{
    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count (how many times user used this tag).
    /// </summary>
    [JsonPropertyName("count")]
    public string? Count { get; set; }

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Attributes for user top tags.
/// </summary>
public class UserTopTagsAttributes
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
}
