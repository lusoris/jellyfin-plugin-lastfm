// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models;

/// <summary>
/// Information about a track to scrobble.
/// </summary>
public class Scrobble
{
    /// <summary>
    /// Gets or sets the track name (required).
    /// </summary>
    public string Track { get; set; }

    /// <summary>
    /// Gets or sets the artist name (required).
    /// </summary>
    public string Artist { get; set; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="Scrobble"/> class.
    /// </summary>
    /// <param name="track">Track name.</param>
    /// <param name="artist">Artist name.</param>
    /// <param name="timestamp">Unix timestamp.</param>
    public Scrobble(string track, string artist, long timestamp)
    {
        Track = track;
        Artist = artist;
        Timestamp = timestamp;
    }
}
