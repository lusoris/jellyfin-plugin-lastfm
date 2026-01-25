// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Utilities;

using System.Security.Cryptography;
using System.Text;
using Interfaces;

/// <summary>
/// Generates MD5 signatures for Last.fm API requests.
/// </summary>
public class SignatureGenerator : ISignatureGenerator
{
    /// <summary>
    /// Parameters that should be excluded from signature calculation.
    /// </summary>
    private static readonly HashSet<string> ExcludedParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "format",
        "callback"
    };

    /// <inheritdoc />
    public string CreateSignature(IDictionary<string, string> parameters, string apiSecret)
    {
        // Sort parameters alphabetically by key, excluding format and callback
        var sortedParams = parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && !ExcludedParameters.Contains(kvp.Key))
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
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(inputBytes);

        // Convert to uppercase hex string (Last.fm requires uppercase)
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
    }
}
