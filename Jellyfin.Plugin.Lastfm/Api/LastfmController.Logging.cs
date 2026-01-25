// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Api;

using Microsoft.Extensions.Logging;

/// <summary>
/// LastfmController logger messages.
/// </summary>
public sealed partial class LastfmController
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Authenticating user {UserId} with Last.fm username {LastfmUsername}")]
    private partial void LogAuthenticating(Guid userId, string lastfmUsername);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Last.fm authentication failed for {Username}: {Error}")]
    private partial void LogAuthenticationFailed(string username, string error);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Last.fm authentication successful for {Username}")]
    private partial void LogAuthenticationSuccess(string username);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Disconnecting user {UserId} from Last.fm account {Username}")]
    private partial void LogDisconnecting(Guid userId, string username);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Creating similar artists playlist for user {UserId}")]
    private partial void LogCreatingSimilarArtistsPlaylist(Guid userId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Creating similar tracks playlist for user {UserId}")]
    private partial void LogCreatingSimilarTracksPlaylist(Guid userId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Creating rediscover favorites playlist for user {UserId}")]
    private partial void LogCreatingRediscoverPlaylist(Guid userId);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Creating weekly mixtape for user {UserId}")]
    private partial void LogCreatingWeeklyMixtape(Guid userId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Information,
        Message = "Creating tag discovery playlist for user {UserId}")]
    private partial void LogCreatingTagDiscoveryPlaylist(Guid userId);
}
