// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Buffers;
using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Generates MD5 signatures for Last.fm API requests.
/// </summary>
public class SignatureGenerator : ISignatureGenerator
{
    /// <summary>
    /// Stack allocation threshold for MD5 input buffer.
    /// </summary>
    private const int StackAllocThreshold = 512;

    /// <summary>
    /// Parameters that should be excluded from signature calculation.
    /// FrozenSet provides faster lookups than HashSet for static data.
    /// </summary>
    private static readonly FrozenSet<string> ExcludedParameters = FrozenSet.ToFrozenSet(
        ["format", "callback"],
        StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string CreateSignature(IDictionary<string, string> parameters, string apiSecret)
    {
        // Sort parameters alphabetically by key, excluding format and callback
        var sortedParams = parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && !ExcludedParameters.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal);

        // Estimate capacity to reduce reallocations
        var estimatedCapacity = (parameters.Count * 20) + apiSecret.Length;
        var sb = new StringBuilder(estimatedCapacity);

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
        var byteCount = Encoding.UTF8.GetByteCount(input);

        // Use stackalloc for small inputs, ArrayPool for larger ones
        if (byteCount <= StackAllocThreshold)
        {
            Span<byte> inputBytes = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(input, inputBytes);

            Span<byte> hashBytes = stackalloc byte[16]; // MD5 produces 16 bytes
            MD5.HashData(inputBytes, hashBytes);

            return Convert.ToHexString(hashBytes);
        }
        else
        {
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var written = Encoding.UTF8.GetBytes(input, rentedBuffer);
                var inputBytes = rentedBuffer.AsSpan(0, written);
                var hashBytes = MD5.HashData(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }
}
