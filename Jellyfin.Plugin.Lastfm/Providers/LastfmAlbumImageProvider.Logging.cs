// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Providers;

using Microsoft.Extensions.Logging;

/// <summary>
/// LastfmAlbumImageProvider logger messages.
/// </summary>
public sealed partial class LastfmAlbumImageProvider
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "No album name or MBID available for album lookup")]
    private partial void LogNoAlbumInfo();

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Fetching Last.fm images for album {ArtistName} - {AlbumName} (MBID: {Mbid})")]
    private partial void LogFetchingImages(string artistName, string albumName, string mbid);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "No images found for album {AlbumName}")]
    private partial void LogNoImagesFound(string albumName);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Found {Size} image for album {AlbumName}: {Url}")]
    private partial void LogFoundImage(string size, string albumName, string url);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Error fetching images for album {AlbumName}")]
    private partial void LogFetchError(Exception ex, string albumName);
}
