// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from auth.getMobileSession.
/// </summary>
public class MobileSessionResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the session data.
    /// </summary>
    [JsonPropertyName("session")]
    public MobileSession? Session { get; set; }
}

/// <summary>
/// Session data from authentication.
/// </summary>
public class MobileSession
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session key.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscriber status.
    /// </summary>
    [JsonPropertyName("subscriber")]
    public int Subscriber { get; set; }
}
