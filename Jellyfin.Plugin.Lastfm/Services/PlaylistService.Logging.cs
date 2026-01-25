// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using Microsoft.Extensions.Logging;

/// <summary>
/// PlaylistService logger messages.
/// </summary>
public sealed partial class PlaylistService
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Creating similar artists playlist for {Username}")]
    private partial void LogCreatingSimilarArtistsPlaylist(string username);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Creating similar tracks playlist for {Username}")]
    private partial void LogCreatingSimilarTracksPlaylist(string username);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Creating rediscover favorites playlist for {Username}")]
    private partial void LogCreatingRediscoverPlaylist(string username);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Creating weekly mixtape for {Username}")]
    private partial void LogCreatingWeeklyMixtape(string username);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Creating tag discovery playlist for {Username}")]
    private partial void LogCreatingTagDiscoveryPlaylist(string username);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Fetching tracks for tag: {Tag}")]
    private partial void LogFetchingTracksForTag(string tag);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Created playlist '{PlaylistName}' with {TrackCount} tracks for user {UserId}")]
    private partial void LogPlaylistCreated(string playlistName, int trackCount, Guid userId);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Failed to create playlist '{PlaylistName}'")]
    private partial void LogPlaylistCreationFailed(Exception ex, string playlistName);
}
