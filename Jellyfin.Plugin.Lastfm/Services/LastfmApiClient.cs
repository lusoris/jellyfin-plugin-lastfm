// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using Lastfm.Scrobbler.Core.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm.Services;

/// <summary>
/// Client for the Last.fm API.
/// </summary>
public sealed partial class LastfmApiClient : ILastfmApiClient, IDisposable
{
    private const string ApiBaseUrl = "https://ws.audioscrobbler.com/2.0/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISignatureGenerator _signatureGenerator;
    private readonly ILogger<LastfmApiClient> _logger;
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmApiClient"/> class.
    /// </summary>
    public LastfmApiClient(
        IHttpClientFactory httpClientFactory,
        ISignatureGenerator signatureGenerator,
        ILogger<LastfmApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _signatureGenerator = signatureGenerator;
        _logger = logger;
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
        Scrobble scrobble,
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
            LogScrobbleRejected(
                scrobble.Artist,
                scrobble.Track,
                response?.Error?.Message ?? "Unknown error");
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<ScrobbleResponse?> ScrobbleBatchAsync(
        IReadOnlyList<Scrobble> scrobbles,
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
            LogBatchScrobbleComplete(
                response.Scrobbles.Attributes.Accepted,
                response.Scrobbles.Attributes.Ignored);
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateNowPlayingAsync(
        Scrobble scrobble,
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        LogUpdatingNowPlaying(scrobble.Artist, scrobble.Track);

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
            LogNowPlayingUpdated(scrobble.Artist, scrobble.Track);
        }
        else
        {
            LogNowPlayingFailed(
                scrobble.Artist,
                scrobble.Track,
                response?.Error?.Message ?? "Unknown error");
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
            LogLoveTrackSuccess(artist, track);
        }
        else
        {
            LogLoveTrackFailed(
                artist,
                track,
                response?.Error?.Message ?? "Unknown error");
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
            LogUnloveTrackSuccess(artist, track);
        }
        else
        {
            LogUnloveTrackFailed(
                artist,
                track,
                response?.Error?.Message ?? "Unknown error");
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
            LogFetchedLovedTracks(
                response.LovedTracks?.Tracks?.Count ?? 0,
                username);
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
            LogFetchedTopTracks(
                response.TopTracks?.Tracks?.Count ?? 0,
                username);
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
            LogFetchedSimilarArtists(
                response.SimilarArtists.Artists.Count,
                artist);
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
            LogFetchedSimilarTracks(
                response.SimilarTracks.Tracks.Count,
                artist,
                track);
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
            LogFetchedAlbumInfo(response.Album.Artist ?? "Unknown", response.Album.Name ?? "Unknown");
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
        LogFetchingTagTracks(tag);

        var url = $"{ApiBaseUrl}?method=tag.getTopTracks" +
                  $"&tag={Uri.EscapeDataString(tag)}" +
                  $"&limit={limit}" +
                  $"&page={page}" +
                  $"&api_key={GetApiKey()}" +
                  $"&format=json";

        var response = await GetAsync<TagTopTracksResponse>(url, cancellationToken).ConfigureAwait(false);

        if (response?.Tracks?.TrackList != null)
        {
            LogFetchedTagTracks(response.Tracks.TrackList.Count, tag);
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
                using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(json))
                {
                    LogEmptyResponse();
                    return null;
                }

                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);

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
                    LogTemporaryServiceError(result.Error.Code);
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
                LogJsonParseError(ex);
                return null;
            }
        }

        LogRequestFailedAfterRetries(maxRetries);
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
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minimumDelay = TimeSpan.FromSeconds(1);

            if (timeSinceLastRequest < minimumDelay)
            {
                var waitTime = minimumDelay - timeSinceLastRequest;
                await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
            }

            _lastRequestTime = DateTime.UtcNow;
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

    /// <summary>
    /// Disposes the API client and releases resources.
    /// </summary>
    public void Dispose()
    {
        _rateLimiter.Dispose();
    }
}
