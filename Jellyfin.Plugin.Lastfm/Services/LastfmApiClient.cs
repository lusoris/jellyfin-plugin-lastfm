// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Models;
using Models.Responses;

/// <summary>
/// Client for the Last.fm API.
/// </summary>
public class LastfmApiClient : ILastfmApiClient
{
    private const string ApiBaseUrl = "https://ws.audioscrobbler.com/2.0/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISignatureGenerator _signatureGenerator;
    private readonly ILogger<LastfmApiClient> _logger;

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
    public Task<MobileSessionResponse?> GetMobileSessionAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // TODO: Implement auth.getMobileSession
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<ScrobbleResponse?> ScrobbleAsync(ScrobbleInfo scrobble, string sessionKey, CancellationToken cancellationToken = default)
    {
        // TODO: Implement track.scrobble
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<bool> UpdateNowPlayingAsync(ScrobbleInfo scrobble, string sessionKey, CancellationToken cancellationToken = default)
    {
        // TODO: Implement track.updateNowPlaying
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<bool> LoveTrackAsync(string artist, string track, string sessionKey, CancellationToken cancellationToken = default)
    {
        // TODO: Implement track.love
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<bool> UnloveTrackAsync(string artist, string track, string sessionKey, CancellationToken cancellationToken = default)
    {
        // TODO: Implement track.unlove
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<LovedTracksResponse?> GetLovedTracksAsync(string username, int page = 1, int limit = 1000, CancellationToken cancellationToken = default)
    {
        // TODO: Implement user.getLovedTracks
        throw new NotImplementedException();
    }

    private string GetApiKey()
    {
        // TODO: Get from configuration
        return Plugin.Instance?.Configuration.ApiKey ?? string.Empty;
    }

    private string GetApiSecret()
    {
        // TODO: Get from configuration
        return Plugin.Instance?.Configuration.ApiSecret ?? string.Empty;
    }
}
