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
    /// Gets or sets the error information.
    /// </summary>
    [JsonPropertyName("error")]
    public int? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the error information if present.
    /// </summary>
    [JsonIgnore]
    public LastfmError? Error => ErrorCode.HasValue
        ? new LastfmError { Code = ErrorCode.Value, Message = ErrorMessage ?? "Unknown error" }
        : null;

    /// <summary>
    /// Gets a value indicating whether the response is an error.
    /// </summary>
    [JsonIgnore]
    public bool IsError => ErrorCode.HasValue && ErrorCode > 0;

    /// <summary>
    /// Gets a value indicating whether the request was successful.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => !IsError;
}

/// <summary>
/// Error information from Last.fm API.
/// </summary>
public class LastfmError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
