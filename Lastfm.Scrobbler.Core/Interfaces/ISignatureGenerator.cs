using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Interfaces;

/// <summary>
/// Interface for generating API request signatures.
/// </summary>
public interface ISignatureGenerator
{
    /// <summary>
    /// Creates an MD5 signature for Last.fm API authentication.
    /// </summary>
    /// <param name="parameters">The request parameters to sign.</param>
    /// <param name="secret">The API secret key.</param>
    /// <returns>The MD5 signature string.</returns>
    string CreateSignature(IDictionary<string, string> parameters, string secret);
}
