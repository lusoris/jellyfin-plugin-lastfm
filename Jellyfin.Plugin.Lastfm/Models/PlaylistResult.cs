// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models;

/// <summary>
/// Result of a playlist creation operation.
/// </summary>
public class PlaylistResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the playlist ID if created successfully.
    /// </summary>
    public Guid? PlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets the number of tracks added to the playlist.
    /// </summary>
    public int TracksAdded { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Creates a successful playlist result.
    /// </summary>
    public static PlaylistResult SuccessResult(Guid playlistId, string playlistName, int tracksAdded)
    {
        return new PlaylistResult
        {
            Success = true,
            PlaylistId = playlistId,
            PlaylistName = playlistName,
            TracksAdded = tracksAdded
        };
    }

    /// <summary>
    /// Creates a failure playlist result.
    /// </summary>
    public static PlaylistResult FailureResult(string error)
    {
        return new PlaylistResult
        {
            Success = false,
            Error = error
        };
    }
}
