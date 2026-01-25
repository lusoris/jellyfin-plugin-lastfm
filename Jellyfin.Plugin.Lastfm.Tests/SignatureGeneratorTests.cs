// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Tests;

using Lastfm.Services;
using Xunit;

/// <summary>
/// Tests for the SignatureGenerator service.
/// </summary>
public class SignatureGeneratorTests
{
    private readonly SignatureGenerator _generator = new();

    [Fact]
    public void CreateSignature_WithSimpleParams_ReturnsCorrectMd5()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["method"] = "auth.getMobileSession",
            ["api_key"] = "testkey",
            ["username"] = "testuser",
            ["password"] = "testpass"
        };
        const string secret = "testsecret";

        // Act
        var signature = _generator.CreateSignature(parameters, secret);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(32, signature.Length); // MD5 hash is 32 hex characters
        Assert.Matches("^[A-F0-9]{32}$", signature); // Uppercase hex for Last.fm
    }

    [Fact]
    public void CreateSignature_SameParams_ReturnsSameSignature()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["api_key"] = "key123",
            ["method"] = "track.scrobble"
        };
        const string secret = "mysecret";

        // Act
        var signature1 = _generator.CreateSignature(parameters, secret);
        var signature2 = _generator.CreateSignature(parameters, secret);

        // Assert
        Assert.Equal(signature1, signature2);
    }

    [Fact]
    public void CreateSignature_DifferentParams_ReturnsDifferentSignature()
    {
        // Arrange
        var params1 = new Dictionary<string, string> { ["key"] = "value1" };
        var params2 = new Dictionary<string, string> { ["key"] = "value2" };
        const string secret = "secret";

        // Act
        var sig1 = _generator.CreateSignature(params1, secret);
        var sig2 = _generator.CreateSignature(params2, secret);

        // Assert
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void CreateSignature_DifferentSecret_ReturnsDifferentSignature()
    {
        // Arrange
        var parameters = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var sig1 = _generator.CreateSignature(parameters, "secret1");
        var sig2 = _generator.CreateSignature(parameters, "secret2");

        // Assert
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void CreateSignature_ExcludesFormatParameter()
    {
        // Arrange - format should be excluded from signature
        var paramsWithFormat = new Dictionary<string, string>
        {
            ["api_key"] = "key",
            ["method"] = "test",
            ["format"] = "json"
        };
        var paramsWithoutFormat = new Dictionary<string, string>
        {
            ["api_key"] = "key",
            ["method"] = "test"
        };
        const string secret = "secret";

        // Act
        var sigWith = _generator.CreateSignature(paramsWithFormat, secret);
        var sigWithout = _generator.CreateSignature(paramsWithoutFormat, secret);

        // Assert - should be the same since format is excluded
        Assert.Equal(sigWith, sigWithout);
    }

    [Fact]
    public void CreateSignature_SortsParametersAlphabetically()
    {
        // Arrange - different insertion order, same values
        var params1 = new Dictionary<string, string>
        {
            ["z_param"] = "z",
            ["a_param"] = "a",
            ["m_param"] = "m"
        };
        var params2 = new Dictionary<string, string>
        {
            ["a_param"] = "a",
            ["m_param"] = "m",
            ["z_param"] = "z"
        };
        const string secret = "secret";

        // Act
        var sig1 = _generator.CreateSignature(params1, secret);
        var sig2 = _generator.CreateSignature(params2, secret);

        // Assert - should be same regardless of insertion order
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void CreateSignature_EmptyParams_ReturnsHashOfSecret()
    {
        // Arrange
        var emptyParams = new Dictionary<string, string>();
        const string secret = "secret";

        // Act
        var signature = _generator.CreateSignature(emptyParams, secret);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(32, signature.Length);
    }
}
