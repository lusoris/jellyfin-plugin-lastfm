// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Providers;

using Microsoft.Extensions.Logging;

/// <summary>
/// LastfmArtistImageProvider logger messages.
/// </summary>
public sealed partial class LastfmArtistImageProvider
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Fetching Last.fm images for artist {ArtistName} (MBID: {Mbid})")]
    private partial void LogFetchingImages(string artistName, string mbid);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "No images found for artist {ArtistName}")]
    private partial void LogNoImagesFound(string artistName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Found {Size} image for artist {ArtistName}: {Url}")]
    private partial void LogFoundImage(string size, string artistName, string url);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Error fetching images for artist {ArtistName}")]
    private partial void LogFetchError(Exception ex, string artistName);
}
