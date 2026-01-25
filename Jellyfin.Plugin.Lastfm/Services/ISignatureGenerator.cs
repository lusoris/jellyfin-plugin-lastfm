// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

/// <summary>
/// Generates MD5 signatures for Last.fm API requests.
/// </summary>
public interface ISignatureGenerator
{
    /// <summary>
    /// Creates the api_sig parameter for authenticated Last.fm requests.
    /// </summary>
    /// <param name="parameters">Request parameters (excluding api_sig).</param>
    /// <param name="apiSecret">The Last.fm API secret.</param>
    /// <returns>MD5 hash signature (lowercase hex, as required by Last.fm API).</returns>
    string CreateSignature(IDictionary<string, string> parameters, string apiSecret);

    /// <summary>
    /// Creates an MD5 hash of the input string.
    /// </summary>
    /// <param name="input">String to hash.</param>
    /// <returns>MD5 hash (lowercase hex, as required by Last.fm API).</returns>
    string CreateMd5Hash(string input);
}
