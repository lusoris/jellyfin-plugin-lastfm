// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response from user.getWeeklyTrackChart API call.
/// </summary>
public class WeeklyTrackChartResponse : BaseResponse
{
    /// <summary>
    /// Gets or sets the weekly track chart.
    /// </summary>
    [JsonPropertyName("weeklytrackchart")]
    public WeeklyTrackChartData? WeeklyTrackChart { get; set; }
}

/// <summary>
/// Weekly track chart data.
/// </summary>
public class WeeklyTrackChartData
{
    /// <summary>
    /// Gets or sets the list of tracks.
    /// </summary>
    [JsonPropertyName("track")]
    public List<WeeklyChartTrack>? Tracks { get; set; }

    /// <summary>
    /// Gets or sets the attributes containing user and date info.
    /// </summary>
    [JsonPropertyName("@attr")]
    public WeeklyChartAttributes? Attributes { get; set; }
}

/// <summary>
/// Track in weekly chart.
/// </summary>
public class WeeklyChartTrack
{
    /// <summary>
    /// Gets or sets the track name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist info.
    /// </summary>
    [JsonPropertyName("artist")]
    public WeeklyChartArtist? Artist { get; set; }

    /// <summary>
    /// Gets or sets the MusicBrainz ID.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? Mbid { get; set; }

    /// <summary>
    /// Gets or sets the play count.
    /// </summary>
    [JsonPropertyName("playcount")]
    public string? Playcount { get; set; }

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Artist info in weekly chart track.
/// </summary>
public class WeeklyChartArtist
{
    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    [JsonPropertyName("#text")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MusicBrainz ID.
    /// </summary>
    [JsonPropertyName("mbid")]
    public string? Mbid { get; set; }
}

/// <summary>
/// Attributes for weekly chart.
/// </summary>
public class WeeklyChartAttributes
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the start timestamp.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the end timestamp.
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }
}
