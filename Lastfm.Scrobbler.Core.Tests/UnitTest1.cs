// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Xunit;

namespace Lastfm.Scrobbler.Core.Tests;

public class SignatureGeneratorTests
{
    [Fact]
    public void CreateSignature_WithValidParams_ReturnsLowercaseMd5()
    {
        // Arrange
        var generator = new Utilities.SignatureGenerator();
        var parameters = new Dictionary<string, string>
        {
            ["api_key"] = "test_key",
            ["method"] = "track.scrobble",
            ["artist"] = "Test Artist",
            ["track"] = "Test Track"
        };
        var apiSecret = "test_secret";

        // Act
        var signature = generator.CreateSignature(parameters, apiSecret);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(32, signature.Length); // MD5 hex is 32 chars
        Assert.Equal(signature.ToLowerInvariant(), signature); // Must be lowercase
    }

    [Fact]
    public void CreateSignature_SameInputs_ReturnsSameHash()
    {
        // Arrange
        var generator = new Utilities.SignatureGenerator();
        var parameters = new Dictionary<string, string>
        {
            ["api_key"] = "key123",
            ["method"] = "auth.getMobileSession"
        };
        var apiSecret = "secret123";

        // Act
        var signature1 = generator.CreateSignature(parameters, apiSecret);
        var signature2 = generator.CreateSignature(parameters, apiSecret);

        // Assert
        Assert.Equal(signature1, signature2);
    }
}
