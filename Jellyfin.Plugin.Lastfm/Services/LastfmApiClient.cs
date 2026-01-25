// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Buffers;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Models.Responses;

/// <summary>
/// Client for the Last.fm API.
/// </summary>
public sealed partial class LastfmApiClient : ILastfmApiClient, IDisposable
{
    private const string ApiBaseUrl = "https://ws.audioscrobbler.com/2.0/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISignatureGenerator _signatureGenerator;
    private readonly ILogger<LastfmApiClient> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTimeOffset _lastRequestTime = DateTimeOffset.MinValue;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmApiClient"/> class.
    /// </summary>
    public LastfmApiClient(
        IHttpClientFactory httpClientFactory,
        ISignatureGenerator signatureGenerator,
        ILogger<LastfmApiClient> logger,
        TimeProvider? timeProvider = null)
    {
        _httpClientFactory = httpClientFactory;
        _signatureGenerator = signatureGenerator;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<MobileSessionResponse?> GetMobileSessionAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        LogAuthenticating(username);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "auth.getMobileSession",
            ["username"] = username,
            ["password"] = password,
            ["api_key"] = GetApiKey()
        };

        var response = await PostSignedAsync<MobileSessionResponse>(parameters, cancellationToken).ConfigureAwait(false);

        if (response?.Session != null)
        {
            LogAuthenticationSuccess(username);
        }
        else
        {
            LogAuthenticationFailed(username, response?.Error?.Message);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<ScrobbleResponse?> ScrobbleAsync(
        ScrobbleInfo scrobble,
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        LogScrobbling(scrobble.Artist, scrobble.Track);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.scrobble",
            ["artist"] = scrobble.Artist,
            ["track"] = scrobble.Track,
            ["timestamp"] = scrobble.Timestamp.ToString(CultureInfo.InvariantCulture),
            ["api_key"] = GetApiKey(),
            ["sk"] = sessionKey
        };

        // Add optional parameters
        if (!string.IsNullOrEmpty(scrobble.Album))
        {
            parameters["album"] = scrobble.Album;
        }

        if (!string.IsNullOrEmpty(scrobble.AlbumArtist))
        {
            parameters["albumArtist"] = scrobble.AlbumArtist;
        }

        if (!string.IsNullOrEmpty(scrobble.MusicBrainzId))
        {
            parameters["mbid"] = scrobble.MusicBrainzId;
        }

        if (scrobble.Duration.HasValue)
        {
            parameters["duration"] = scrobble.Duration.Value.ToString(CultureInfo.InvariantCulture);
        }

        var response = await PostSignedAsync<ScrobbleResponse>(parameters, cancellationToken).ConfigureAwait(false);

        if (response?.Scrobbles?.Attributes?.Accepted > 0)
        {
            LogScrobbleSuccess(scrobble.Artist, scrobble.Track);
        }
        else
        {
            LogScrobbleRejected(scrobble.Artist, scrobble.Track, response?.Error?.Message ?? "Unknown error");
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<ScrobbleResponse?> ScrobbleBatchAsync(
        IReadOnlyList<ScrobbleInfo> scrobbles,
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        if (scrobbles.Count == 0)
        {
            return null;
        }

        if (scrobbles.Count > 50)
        {
            throw new ArgumentException("Cannot scrobble more than 50 tracks at once", nameof(scrobbles));
        }

        LogBatchScrobbling(scrobbles.Count);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.scrobble",
            ["api_key"] = GetApiKey(),
            ["sk"] = sessionKey
        };

        // Add indexed parameters for each track
        for (var i = 0; i < scrobbles.Count; i++)
        {
            var scrobble = scrobbles[i];
            parameters[$"artist[{i}]"] = scrobble.Artist;
            parameters[$"track[{i}]"] = scrobble.Track;
            parameters[$"timestamp[{i}]"] = scrobble.Timestamp.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(scrobble.Album))
            {
                parameters[$"album[{i}]"] = scrobble.Album;
            }

            if (!string.IsNullOrEmpty(scrobble.AlbumArtist))
            {
                parameters[$"albumArtist[{i}]"] = scrobble.AlbumArtist;
            }

            if (!string.IsNullOrEmpty(scrobble.MusicBrainzId))
            {
                parameters[$"mbid[{i}]"] = scrobble.MusicBrainzId;
            }

            if (scrobble.Duration.HasValue)
            {
                parameters[$"duration[{i}]"] = scrobble.Duration.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        var response = await PostSignedAsync<ScrobbleResponse>(parameters, cancellationToken).ConfigureAwait(false);

        if (response?.Scrobbles?.Attributes != null)
        {
            LogBatchScrobbleComplete(response.Scrobbles.Attributes.Accepted, response.Scrobbles.Attributes.Ignored);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateNowPlayingAsync(
        ScrobbleInfo scrobble,
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        LogNowPlayingUpdating(scrobble.Artist, scrobble.Track);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.updateNowPlaying",
            ["artist"] = scrobble.Artist,
            ["track"] = scrobble.Track,
            ["api_key"] = GetApiKey(),
            ["sk"] = sessionKey
        };

        // Add optional parameters
        if (!string.IsNullOrEmpty(scrobble.Album))
        {
            parameters["album"] = scrobble.Album;
        }

        if (!string.IsNullOrEmpty(scrobble.AlbumArtist))
        {
            parameters["albumArtist"] = scrobble.AlbumArtist;
        }

        if (!string.IsNullOrEmpty(scrobble.MusicBrainzId))
        {
            parameters["mbid"] = scrobble.MusicBrainzId;
        }

        if (scrobble.Duration.HasValue)
        {
            parameters["duration"] = scrobble.Duration.Value.ToString(CultureInfo.InvariantCulture);
        }

        var response = await PostSignedAsync<BaseResponse>(parameters, cancellationToken).ConfigureAwait(false);
        var success = response?.IsSuccess ?? false;

        if (success)
        {
            LogNowPlayingSuccess(scrobble.Artist, scrobble.Track);
        }
        else
        {
            LogNowPlayingFailed(scrobble.Artist, scrobble.Track, response?.Error?.Message ?? "Unknown error");
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> LoveTrackAsync(
        string artist,
        string track,
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        LogLovingTrack(artist, track);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.love",
            ["artist"] = artist,
            ["track"] = track,
            ["api_key"] = GetApiKey(),
            ["sk"] = sessionKey
        };

        var response = await PostSignedAsync<BaseResponse>(parameters, cancellationToken).ConfigureAwait(false);
        var success = response?.IsSuccess ?? false;

        if (success)
        {
            LogLovedTrack(artist, track);
        }
        else
        {
            LogLoveTrackFailed(artist, track, response?.Error?.Message ?? "Unknown error");
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> UnloveTrackAsync(
        string artist,
        string track,
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        LogUnlovingTrack(artist, track);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "track.unlove",
            ["artist"] = artist,
            ["track"] = track,
            ["api_key"] = GetApiKey(),
            ["sk"] = sessionKey
        };

        var response = await PostSignedAsync<BaseResponse>(parameters, cancellationToken).ConfigureAwait(false);
        var success = response?.IsSuccess ?? false;

        if (success)
        {
            LogUnlovedTrack(artist, track);
        }
        else
        {
            LogUnloveTrackFailed(artist, track, response?.Error?.Message ?? "Unknown error");
        }

        return success;
    }

    /// <inheritdoc />
    public async Task<LovedTracksResponse?> GetLovedTracksAsync(
        string username,
        int page = 1,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        LogFetchingLovedTracks(username, page);

        var url = $"{ApiBaseUrl}?method=user.getLovedTracks" +
                  $"&user={Uri.EscapeDataString(username)}" +
                  $"&api_key={GetApiKey()}" +
                  $"&page={page}" +
                  $"&limit={limit}" +
                  $"&format=json";

        var response = await GetAsync<LovedTracksResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.HasTracks == true)
        {
            LogFetchedLovedTracks(response.LovedTracks?.Tracks?.Count ?? 0, username);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<TopTracksResponse?> GetTopTracksAsync(
        string username,
        string period = "overall",
        int page = 1,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        LogFetchingTopTracks(username, period, page);

        var url = $"{ApiBaseUrl}?method=user.getTopTracks" +
                  $"&user={Uri.EscapeDataString(username)}" +
                  $"&period={Uri.EscapeDataString(period)}" +
                  $"&api_key={GetApiKey()}" +
                  $"&page={page}" +
                  $"&limit={limit}" +
                  $"&format=json";

        var response = await GetAsync<TopTracksResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.HasTracks == true)
        {
            LogFetchedTopTracks(response.TopTracks?.Tracks?.Count ?? 0, username);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<SimilarArtistsResponse?> GetSimilarArtistsAsync(
        string artist,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        LogFetchingSimilarArtists(artist);

        var url = $"{ApiBaseUrl}?method=artist.getSimilar" +
                  $"&artist={Uri.EscapeDataString(artist)}" +
                  $"&api_key={GetApiKey()}" +
                  $"&limit={limit}" +
                  $"&format=json";

        var response = await GetAsync<SimilarArtistsResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.SimilarArtists?.Artists != null)
        {
            LogFetchedSimilarArtists(response.SimilarArtists.Artists.Count, artist);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<SimilarTracksResponse?> GetSimilarTracksAsync(
        string artist,
        string track,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        LogFetchingSimilarTracks(artist, track);

        var url = $"{ApiBaseUrl}?method=track.getSimilar" +
                  $"&artist={Uri.EscapeDataString(artist)}" +
                  $"&track={Uri.EscapeDataString(track)}" +
                  $"&api_key={GetApiKey()}" +
                  $"&limit={limit}" +
                  $"&format=json";

        var response = await GetAsync<SimilarTracksResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.SimilarTracks?.Tracks != null)
        {
            LogFetchedSimilarTracks(response.SimilarTracks.Tracks.Count, artist, track);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<ArtistInfoResponse?> GetArtistInfoAsync(
        string? artist = null,
        string? mbid = null,
        CancellationToken cancellationToken = default)
    {
        LogFetchingArtistInfo(artist ?? "N/A", mbid ?? "N/A");

        var urlBuilder = new StringBuilder($"{ApiBaseUrl}?method=artist.getInfo&api_key={GetApiKey()}&format=json");

        if (!string.IsNullOrEmpty(mbid))
        {
            urlBuilder.Append($"&mbid={Uri.EscapeDataString(mbid)}");
        }
        else if (!string.IsNullOrEmpty(artist))
        {
            urlBuilder.Append($"&artist={Uri.EscapeDataString(artist)}");
        }
        else
        {
            LogArtistInfoMissingParams();
            return null;
        }

        var response = await GetAsync<ArtistInfoResponse>(urlBuilder.ToString(), cancellationToken).ConfigureAwait(false);

        if (response?.Artist != null)
        {
            LogFetchedArtistInfo(response.Artist.Name);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<AlbumInfoResponse?> GetAlbumInfoAsync(
        string? artist = null,
        string? album = null,
        string? mbid = null,
        CancellationToken cancellationToken = default)
    {
        LogFetchingAlbumInfo(artist ?? "N/A", album ?? "N/A", mbid ?? "N/A");

        var urlBuilder = new StringBuilder($"{ApiBaseUrl}?method=album.getInfo&api_key={GetApiKey()}&format=json");

        if (!string.IsNullOrEmpty(mbid))
        {
            urlBuilder.Append($"&mbid={Uri.EscapeDataString(mbid)}");
        }
        else if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(album))
        {
            urlBuilder.Append($"&artist={Uri.EscapeDataString(artist)}&album={Uri.EscapeDataString(album)}");
        }
        else
        {
            LogAlbumInfoMissingParams();
            return null;
        }

        var response = await GetAsync<AlbumInfoResponse>(urlBuilder.ToString(), cancellationToken).ConfigureAwait(false);

        if (response?.Album != null)
        {
            LogFetchedAlbumInfo(response.Album.Artist, response.Album.Name);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<WeeklyTrackChartResponse?> GetWeeklyTrackChartAsync(
        string username,
        long? from = null,
        long? to = null,
        CancellationToken cancellationToken = default)
    {
        LogFetchingWeeklyChart(username);

        var urlBuilder = new StringBuilder($"{ApiBaseUrl}?method=user.getWeeklyTrackChart");
        urlBuilder.Append($"&user={Uri.EscapeDataString(username)}");
        urlBuilder.Append($"&api_key={GetApiKey()}&format=json");

        if (from.HasValue)
        {
            urlBuilder.Append($"&from={from.Value}");
        }

        if (to.HasValue)
        {
            urlBuilder.Append($"&to={to.Value}");
        }

        var response = await GetAsync<WeeklyTrackChartResponse>(urlBuilder.ToString(), cancellationToken).ConfigureAwait(false);

        if (response?.WeeklyTrackChart?.Tracks != null)
        {
            LogFetchedWeeklyChart(response.WeeklyTrackChart.Tracks.Count, username);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<UserTopTagsResponse?> GetUserTopTagsAsync(
        string username,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        LogFetchingTopTags(username);

        var url = $"{ApiBaseUrl}?method=user.getTopTags" +
                  $"&user={Uri.EscapeDataString(username)}" +
                  $"&limit={limit}" +
                  $"&api_key={GetApiKey()}" +
                  $"&format=json";

        var response = await GetAsync<UserTopTagsResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.TopTags?.Tags != null)
        {
            LogFetchedTopTags(response.TopTags.Tags.Count, username);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<TagTopTracksResponse?> GetTagTopTracksAsync(
        string tag,
        int limit = 50,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        LogFetchingTagTopTracks(tag);

        var url = $"{ApiBaseUrl}?method=tag.getTopTracks" +
                  $"&tag={Uri.EscapeDataString(tag)}" +
                  $"&limit={limit}" +
                  $"&page={page}" +
                  $"&api_key={GetApiKey()}" +
                  $"&format=json";

        var response = await GetAsync<TagTopTracksResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.Tracks?.TrackList != null)
        {
            LogFetchedTagTopTracks(response.Tracks.TrackList.Count, tag);
        }

        return response;
    }

    /// <summary>
    /// Sends a signed POST request to the Last.fm API.
    /// </summary>
    private async Task<T?> PostSignedAsync<T>(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken) where T : BaseResponse
    {
        // Add signature
        var signature = _signatureGenerator.CreateSignature(parameters, GetApiSecret());
        parameters["api_sig"] = signature;
        parameters["format"] = "json";

        // Build form content
        var content = new FormUrlEncodedContent(parameters);

        return await SendRequestAsync<T>(
            () => new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl) { Content = content },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a GET request to the Last.fm API.
    /// </summary>
    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken) where T : BaseResponse
    {
        return await SendRequestAsync<T>(
            () => new HttpRequestMessage(HttpMethod.Get, url),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a request with rate limiting and retry logic.
    /// </summary>
    private async Task<T?> SendRequestAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken) where T : BaseResponse
    {
        await ApplyRateLimitAsync(cancellationToken).ConfigureAwait(false);

        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = requestFactory();
                using var httpClient = _httpClientFactory.CreateClient("LastFm");
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                // Check Content-Length if available (works with streaming)
                if (response.Content.Headers.ContentLength == 0)
                {
                    LogEmptyResponse();
                    return null;
                }

                // Stream deserialization for better memory efficiency
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                var result = await JsonSerializer.DeserializeAsync<T>(
                    stream,
                    LastfmJsonContext.Default.Options,
                    cancellationToken).ConfigureAwait(false);

                // Check for rate limit error
                if (result?.Error?.Code == 29)
                {
                    LogRateLimitExceeded();
                    await Task.Delay(delay * 2, cancellationToken).ConfigureAwait(false);
                    delay *= 2;
                    continue;
                }

                // Check for temporary failures
                if (result?.Error?.Code is 11 or 16)
                {
                    LogTemporaryError(result.Error.Code);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay *= 2;
                    continue;
                }

                return result;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                LogHttpRequestFailed(ex, attempt, maxRetries);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay *= 2;
            }
            catch (JsonException ex)
            {
                LogJsonParseFailed(ex);
                return null;
            }
        }

        LogMaxRetriesExceeded(maxRetries);
        return null;
    }

    /// <summary>
    /// Applies rate limiting (max 1 request per second).
    /// </summary>
    private async Task ApplyRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var timeSinceLastRequest = now - _lastRequestTime;
            var minimumDelay = TimeSpan.FromSeconds(1);

            if (timeSinceLastRequest < minimumDelay)
            {
                var waitTime = minimumDelay - timeSinceLastRequest;
                await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
            }

            _lastRequestTime = _timeProvider.GetUtcNow();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private static string GetApiKey()
    {
        return Plugin.Instance?.Configuration.ApiKey ?? string.Empty;
    }

    private static string GetApiSecret()
    {
        return Plugin.Instance?.Configuration.ApiSecret ?? string.Empty;
    }

    // Source-generated logging methods for improved performance
    [LoggerMessage(Level = LogLevel.Debug, Message = "Authenticating user {Username}")]
    private partial void LogAuthenticating(string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully authenticated user {Username}")]
    private partial void LogAuthenticationSuccess(string username);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Authentication failed for user {Username}: {Error}")]
    private partial void LogAuthenticationFailed(string username, string? error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Scrobbling {Artist} - {Track}")]
    private partial void LogScrobbling(string artist, string track);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully scrobbled {Artist} - {Track}")]
    private partial void LogScrobbleSuccess(string artist, string track);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Scrobble rejected for {Artist} - {Track}: {Error}")]
    private partial void LogScrobbleRejected(string artist, string track, string? error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Batch scrobbling {Count} tracks")]
    private partial void LogBatchScrobbling(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch scrobble complete: {Accepted} accepted, {Ignored} ignored")]
    private partial void LogBatchScrobbleComplete(int accepted, int ignored);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Updating now playing: {Artist} - {Track}")]
    private partial void LogNowPlayingUpdating(string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Now playing updated: {Artist} - {Track}")]
    private partial void LogNowPlayingSuccess(string artist, string track);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update now playing for {Artist} - {Track}: {Error}")]
    private partial void LogNowPlayingFailed(string artist, string track, string? error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loving track: {Artist} - {Track}")]
    private partial void LogLovingTrack(string artist, string track);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loved track: {Artist} - {Track}")]
    private partial void LogLovedTrack(string artist, string track);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to love track {Artist} - {Track}: {Error}")]
    private partial void LogLoveTrackFailed(string artist, string track, string? error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unloving track: {Artist} - {Track}")]
    private partial void LogUnlovingTrack(string artist, string track);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unloved track: {Artist} - {Track}")]
    private partial void LogUnlovedTrack(string artist, string track);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to unlove track {Artist} - {Track}: {Error}")]
    private partial void LogUnloveTrackFailed(string artist, string track, string? error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching loved tracks for {Username}, page {Page}")]
    private partial void LogFetchingLovedTracks(string username, int page);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} loved tracks for {Username}")]
    private partial void LogFetchedLovedTracks(int count, string username);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching top tracks for {Username}, period {Period}, page {Page}")]
    private partial void LogFetchingTopTracks(string username, string period, int page);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} top tracks for {Username}")]
    private partial void LogFetchedTopTracks(int count, string username);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching similar artists for {Artist}")]
    private partial void LogFetchingSimilarArtists(string artist);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} similar artists for {Artist}")]
    private partial void LogFetchedSimilarArtists(int count, string artist);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching similar tracks for {Artist} - {Track}")]
    private partial void LogFetchingSimilarTracks(string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} similar tracks for {Artist} - {Track}")]
    private partial void LogFetchedSimilarTracks(int count, string artist, string track);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching artist info for {Artist} (mbid: {Mbid})")]
    private partial void LogFetchingArtistInfo(string artist, string mbid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GetArtistInfoAsync requires either artist name or mbid")]
    private partial void LogArtistInfoMissingParams();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched artist info for {Artist}")]
    private partial void LogFetchedArtistInfo(string artist);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching album info for {Artist} - {Album} (mbid: {Mbid})")]
    private partial void LogFetchingAlbumInfo(string artist, string album, string mbid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GetAlbumInfoAsync requires either mbid or both artist and album name")]
    private partial void LogAlbumInfoMissingParams();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched album info for {Artist} - {Album}")]
    private partial void LogFetchedAlbumInfo(string? artist, string album);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching weekly track chart for {Username}")]
    private partial void LogFetchingWeeklyChart(string username);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} tracks from weekly chart for {Username}")]
    private partial void LogFetchedWeeklyChart(int count, string username);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching top tags for {Username}")]
    private partial void LogFetchingTopTags(string username);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} top tags for {Username}")]
    private partial void LogFetchedTopTags(int count, string username);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching top tracks for tag {Tag}")]
    private partial void LogFetchingTagTopTracks(string tag);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched {Count} top tracks for tag {Tag}")]
    private partial void LogFetchedTagTopTracks(int count, string tag);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Empty response from Last.fm API")]
    private partial void LogEmptyResponse();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded, waiting before retry")]
    private partial void LogRateLimitExceeded();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Temporary Last.fm service error (code {Code}), retrying")]
    private partial void LogTemporaryError(int code);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HTTP request failed (attempt {Attempt}/{MaxRetries})")]
    private partial void LogHttpRequestFailed(Exception ex, int attempt, int maxRetries);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse Last.fm API response")]
    private partial void LogJsonParseFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Last.fm API request failed after {MaxRetries} retries")]
    private partial void LogMaxRetriesExceeded(int maxRetries);

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _rateLimiter.Dispose();
            _disposed = true;
        }
    }
}
