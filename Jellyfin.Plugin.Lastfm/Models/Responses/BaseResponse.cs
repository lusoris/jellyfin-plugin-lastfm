// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Base response from Last.fm API.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets or sets the error code (0 = no error).
    /// </summary>
    [JsonPropertyName("error")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets a value indicating whether the response is an error.
    /// </summary>
    [JsonIgnore]
    public bool IsError => ErrorCode > 0;
}
