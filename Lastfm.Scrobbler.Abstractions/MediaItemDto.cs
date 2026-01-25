// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Abstractions;

/// <summary>
/// Platform-agnostic representation of a media item (track/audio).
/// </summary>
public sealed class MediaItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the media item.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the track name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    public required string Artist { get; init; }

    /// <summary>
    /// Gets or sets the album name.
    /// </summary>
    public string? Album { get; init; }

    /// <summary>
    /// Gets or sets the album artist name.
    /// </summary>
    public string? AlbumArtist { get; init; }

    /// <summary>
    /// Gets or sets the MusicBrainz Recording ID.
    /// </summary>
    public string? MusicBrainzRecordingId { get; init; }

    /// <summary>
    /// Gets or sets the MusicBrainz Track ID.
    /// </summary>
    public string? MusicBrainzTrackId { get; init; }

    /// <summary>
    /// Gets or sets the runtime in ticks (100ns intervals).
    /// </summary>
    public long? RuntimeTicks { get; init; }

    /// <summary>
    /// Gets the runtime in seconds.
    /// </summary>
    public int? RuntimeSeconds => RuntimeTicks.HasValue ? (int)(RuntimeTicks.Value / TimeSpan.TicksPerSecond) : null;
}
