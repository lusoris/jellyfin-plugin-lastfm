// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models;

/// <summary>
/// Information about a track to scrobble.
/// </summary>
public class ScrobbleInfo
{
    /// <summary>
    /// Gets or sets the track name (required).
    /// </summary>
    public required string Track { get; set; }

    /// <summary>
    /// Gets or sets the artist name (required).
    /// </summary>
    public required string Artist { get; set; }

    /// <summary>
    /// Gets or sets the album name (optional).
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets the album artist (optional).
    /// </summary>
    public string? AlbumArtist { get; set; }

    /// <summary>
    /// Gets or sets the MusicBrainz Track ID (optional).
    /// </summary>
    public string? MusicBrainzId { get; set; }

    /// <summary>
    /// Gets or sets the track duration in seconds (optional).
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the Unix timestamp when the track was played.
    /// </summary>
    public long Timestamp { get; set; }
}
