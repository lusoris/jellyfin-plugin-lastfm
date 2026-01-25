// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Net.Http;
using Lastfm.Scrobbler.Core.Interfaces;

namespace Jellyfin.Plugin.Lastfm.Adapter;

/// <summary>
/// An adapter to bridge the core HTTP client with Jellyfin's IHttpClientFactory.
/// </summary>
public class JellyfinHttpClient : ICoreHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinHttpClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The Jellyfin HTTP client factory.</param>
    public JellyfinHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> PostAsync(string url, FormUrlEncodedContent content)
    {
        var client = _httpClientFactory.CreateClient();
        return client.PostAsync(new Uri(url), content);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        return client.GetAsync(new Uri(url));
    }

    public Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        return client.GetAsync(url, cancellationToken);
    }

    public Task<HttpResponseMessage> PostAsync(string url, FormUrlEncodedContent content, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        return client.PostAsync(url, content, cancellationToken);
    }
}
