// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Generates MD5 signatures for Last.fm API requests.
/// </summary>
public class SignatureGenerator : ISignatureGenerator
{
    /// <inheritdoc />
    public string CreateSignature(IDictionary<string, string> parameters, string apiSecret)
    {
        // Sort parameters alphabetically by key
        var sortedParams = parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal);

        // Concatenate key+value pairs
        var sb = new StringBuilder();
        foreach (var kvp in sortedParams)
        {
            sb.Append(kvp.Key);
            sb.Append(kvp.Value);
        }

        // Append API secret
        sb.Append(apiSecret);

        return CreateMd5Hash(sb.ToString());
    }

    /// <inheritdoc />
    public string CreateMd5Hash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);

        // Convert to uppercase hex string (Last.fm requires uppercase)
        return Convert.ToHexString(hashBytes);
    }
}
