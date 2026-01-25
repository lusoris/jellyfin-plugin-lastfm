// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using Microsoft.Extensions.Logging;

/// <summary>
/// LastfmApiClient logger messages.
/// </summary>
public sealed partial class LastfmApiClient
{
    // Authentication
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Authenticating user {Username}")]
    private partial void LogAuthenticating(string username);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Successfully authenticated user {Username}")]
    private partial void LogAuthenticationSuccess(string username);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Authentication failed for user {Username}: {Error}")]
    private partial void LogAuthenticationFailed(string username, string? error);

    // Scrobble
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Scrobbling {Artist} - {Track}")]
    private partial void LogScrobbling(string artist, string track);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Successfully scrobbled {Artist} - {Track}")]
    private partial void LogScrobbleSuccess(string artist, string track);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Scrobble rejected for {Artist} - {Track}: {Error}")]
    private partial void LogScrobbleRejected(string artist, string track, string error);

    // Batch Scrobble
    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Batch scrobbling {Count} tracks")]
    private partial void LogBatchScrobbling(int count);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Batch scrobble complete: {Accepted} accepted, {Ignored} ignored")]
    private partial void LogBatchScrobbleComplete(int accepted, int ignored);

    // Now Playing
    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Updating now playing: {Artist} - {Track}")]
    private partial void LogUpdatingNowPlaying(string artist, string track);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Now playing updated: {Artist} - {Track}")]
    private partial void LogNowPlayingUpdated(string artist, string track);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Warning,
        Message = "Failed to update now playing for {Artist} - {Track}: {Error}")]
    private partial void LogNowPlayingFailed(string artist, string track, string error);

    // Love/Unlove Track
    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Debug,
        Message = "Loving track: {Artist} - {Track}")]
    private partial void LogLovingTrack(string artist, string track);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Information,
        Message = "Loved track: {Artist} - {Track}")]
    private partial void LogLoveTrackSuccess(string artist, string track);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Warning,
        Message = "Failed to love track {Artist} - {Track}: {Error}")]
    private partial void LogLoveTrackFailed(string artist, string track, string error);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Debug,
        Message = "Unloving track: {Artist} - {Track}")]
    private partial void LogUnlovingTrack(string artist, string track);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Information,
        Message = "Unloved track: {Artist} - {Track}")]
    private partial void LogUnloveTrackSuccess(string artist, string track);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Warning,
        Message = "Failed to unlove track {Artist} - {Track}: {Error}")]
    private partial void LogUnloveTrackFailed(string artist, string track, string error);

    // Get Loved Tracks
    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Debug,
        Message = "Fetching loved tracks for {Username}, page {Page}")]
    private partial void LogFetchingLovedTracks(string username, int page);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} loved tracks for {Username}")]
    private partial void LogFetchedLovedTracks(int count, string username);

    // Get Top Tracks
    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Debug,
        Message = "Fetching top tracks for {Username}, period {Period}, page {Page}")]
    private partial void LogFetchingTopTracks(string username, string period, int page);

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} top tracks for {Username}")]
    private partial void LogFetchedTopTracks(int count, string username);

    // Get Similar Artists
    [LoggerMessage(
        EventId = 22,
        Level = LogLevel.Debug,
        Message = "Fetching similar artists for {Artist}")]
    private partial void LogFetchingSimilarArtists(string artist);

    [LoggerMessage(
        EventId = 23,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} similar artists for {Artist}")]
    private partial void LogFetchedSimilarArtists(int count, string artist);

    // Get Similar Tracks
    [LoggerMessage(
        EventId = 24,
        Level = LogLevel.Debug,
        Message = "Fetching similar tracks for {Artist} - {Track}")]
    private partial void LogFetchingSimilarTracks(string artist, string track);

    [LoggerMessage(
        EventId = 25,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} similar tracks for {Artist} - {Track}")]
    private partial void LogFetchedSimilarTracks(int count, string artist, string track);

    // Get Artist Info
    [LoggerMessage(
        EventId = 26,
        Level = LogLevel.Debug,
        Message = "Fetching artist info for {Artist} (mbid: {Mbid})")]
    private partial void LogFetchingArtistInfo(string artist, string mbid);

    [LoggerMessage(
        EventId = 27,
        Level = LogLevel.Warning,
        Message = "GetArtistInfoAsync requires either artist name or mbid")]
    private partial void LogArtistInfoMissingParams();

    [LoggerMessage(
        EventId = 28,
        Level = LogLevel.Debug,
        Message = "Fetched artist info for {Artist}")]
    private partial void LogFetchedArtistInfo(string artist);

    // Get Album Info
    [LoggerMessage(
        EventId = 29,
        Level = LogLevel.Debug,
        Message = "Fetching album info for {Artist} - {Album} (mbid: {Mbid})")]
    private partial void LogFetchingAlbumInfo(string artist, string album, string mbid);

    [LoggerMessage(
        EventId = 30,
        Level = LogLevel.Warning,
        Message = "GetAlbumInfoAsync requires either mbid or both artist and album name")]
    private partial void LogAlbumInfoMissingParams();

    [LoggerMessage(
        EventId = 31,
        Level = LogLevel.Debug,
        Message = "Fetched album info for {Artist} - {Album}")]
    private partial void LogFetchedAlbumInfo(string artist, string album);

    // Get Weekly Track Chart
    [LoggerMessage(
        EventId = 32,
        Level = LogLevel.Debug,
        Message = "Fetching weekly track chart for {Username}")]
    private partial void LogFetchingWeeklyChart(string username);

    [LoggerMessage(
        EventId = 33,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} tracks from weekly chart for {Username}")]
    private partial void LogFetchedWeeklyChart(int count, string username);

    // Get Top Tags
    [LoggerMessage(
        EventId = 34,
        Level = LogLevel.Debug,
        Message = "Fetching top tags for {Username}")]
    private partial void LogFetchingTopTags(string username);

    [LoggerMessage(
        EventId = 35,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} top tags for {Username}")]
    private partial void LogFetchedTopTags(int count, string username);

    // Get Tag Top Tracks
    [LoggerMessage(
        EventId = 36,
        Level = LogLevel.Debug,
        Message = "Fetching top tracks for tag {Tag}")]
    private partial void LogFetchingTagTracks(string tag);

    [LoggerMessage(
        EventId = 37,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} top tracks for tag {Tag}")]
    private partial void LogFetchedTagTracks(int count, string tag);

    // HTTP Request Errors
    [LoggerMessage(
        EventId = 38,
        Level = LogLevel.Warning,
        Message = "Empty response from Last.fm API")]
    private partial void LogEmptyResponse();

    [LoggerMessage(
        EventId = 39,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded, waiting before retry")]
    private partial void LogRateLimitExceeded();

    [LoggerMessage(
        EventId = 40,
        Level = LogLevel.Warning,
        Message = "Temporary Last.fm service error (code {Code}), retrying")]
    private partial void LogTemporaryServiceError(int code);

    [LoggerMessage(
        EventId = 41,
        Level = LogLevel.Warning,
        Message = "HTTP request failed (attempt {Attempt}/{MaxRetries})")]
    private partial void LogHttpRequestFailed(Exception ex, int attempt, int maxRetries);

    [LoggerMessage(
        EventId = 42,
        Level = LogLevel.Error,
        Message = "Failed to parse Last.fm API response")]
    private partial void LogJsonParseError(Exception ex);

    [LoggerMessage(
        EventId = 43,
        Level = LogLevel.Error,
        Message = "Last.fm API request failed after {MaxRetries} retries")]
    private partial void LogRequestFailedAfterRetries(int maxRetries);
}
