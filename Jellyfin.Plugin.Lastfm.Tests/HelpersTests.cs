namespace Jellyfin.Plugin.Lastfm.Tests;

using Jellyfin.Plugin.Lastfm.Utils;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for the Helpers utility class.
/// </summary>
public class HelpersTests
{
    /// <summary>
    /// Tests MD5 hash generation used for Last.fm API authentication.
    /// The hash MUST be uppercase to match Last.fm API requirements.
    /// </summary>
    [Theory]
    [InlineData("test", "098F6BCD4621D373CADE4E832627B4F6")]
    [InlineData("", "D41D8CD98F00B204E9800998ECF8427E")]
    public void CreateMd5Hash_WithInput_ReturnsExpectedHash(string input, string expected)
    {
        // Act
        var result = Helpers.CreateMd5Hash(input);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that MD5 hash is uppercase (required by Last.fm API).
    /// </summary>
    [Fact]
    public void CreateMd5Hash_ReturnsUppercaseHash()
    {
        // Arrange
        var input = "TEST";

        // Act
        var result = Helpers.CreateMd5Hash(input);

        // Assert
        result.ShouldBe(result.ToUpperInvariant());
    }

    /// <summary>
    /// Tests that MD5 hash has correct length (32 hex characters).
    /// </summary>
    [Fact]
    public void CreateMd5Hash_ReturnsCorrectLength()
    {
        // Arrange
        var input = "any input";

        // Act
        var result = Helpers.CreateMd5Hash(input);

        // Assert
        result.Length.ShouldBe(32);
        result.ShouldMatch("^[A-F0-9]{32}$");
    }

    /// <summary>
    /// Tests that AppendSignature adds api_sig key to dictionary.
    /// Note: Uses hardcoded API secret from Strings.Keys.LastfmApiSecret.
    /// </summary>
    [Fact]
    public void AppendSignature_AddsApiSigKey()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            { "method", "track.scrobble" },
            { "api_key", "testkey" }
        };

        // Act
        Helpers.AppendSignature(ref parameters);

        // Assert
        parameters.ShouldContainKey("api_sig");
        parameters["api_sig"].Length.ShouldBe(32);
        parameters["api_sig"].ShouldMatch("^[A-F0-9]{32}$");
    }

    /// <summary>
    /// Tests that signature is consistent for same input (deterministic).
    /// </summary>
    [Fact]
    public void AppendSignature_IsDeterministic()
    {
        // Arrange
        var params1 = new Dictionary<string, string>
        {
            { "method", "test" },
            { "api_key", "key" }
        };
        var params2 = new Dictionary<string, string>
        {
            { "method", "test" },
            { "api_key", "key" }
        };

        // Act
        Helpers.AppendSignature(ref params1);
        Helpers.AppendSignature(ref params2);

        // Assert
        params1["api_sig"].ShouldBe(params2["api_sig"]);
    }

    /// <summary>
    /// Tests that parameter order doesn't affect signature (params are sorted internally).
    /// </summary>
    [Fact]
    public void AppendSignature_OrderIndependent()
    {
        // Arrange
        var params1 = new Dictionary<string, string>
        {
            { "a", "1" },
            { "b", "2" },
            { "c", "3" }
        };
        var params2 = new Dictionary<string, string>
        {
            { "c", "3" },
            { "a", "1" },
            { "b", "2" }
        };

        // Act
        Helpers.AppendSignature(ref params1);
        Helpers.AppendSignature(ref params2);

        // Assert
        params1["api_sig"].ShouldBe(params2["api_sig"]);
    }

    /// <summary>
    /// Tests Unix timestamp conversion.
    /// </summary>
    [Fact]
    public void ToTimestamp_ReturnsValidUnixTimestamp()
    {
        // Arrange - use a known date
        var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = Helpers.ToTimestamp(date);

        // Assert - Jan 1, 2024 00:00:00 UTC = 1704067200
        result.ShouldBe(1704067200);
    }

    /// <summary>
    /// Tests Unix epoch returns 0.
    /// </summary>
    [Fact]
    public void ToTimestamp_UnixEpoch_ReturnsZero()
    {
        // Arrange
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = Helpers.ToTimestamp(epoch);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Tests FromTimestamp converts back to DateTime.
    /// </summary>
    [Fact]
    public void FromTimestamp_ConvertsToDateTime()
    {
        // Arrange
        double timestamp = 1704067200; // Jan 1, 2024 00:00:00 UTC

        // Act
        var result = Helpers.FromTimestamp(timestamp);

        // Assert - note: FromTimestamp converts to local time
        result.ToUniversalTime().Year.ShouldBe(2024);
        result.ToUniversalTime().Month.ShouldBe(1);
        result.ToUniversalTime().Day.ShouldBe(1);
    }

    /// <summary>
    /// Tests CurrentTimestamp returns a reasonable value.
    /// </summary>
    [Fact]
    public void CurrentTimestamp_ReturnsReasonableValue()
    {
        // Arrange - we know current time is after Jan 1, 2024
        var minExpected = 1704067200; // Jan 1, 2024

        // Act
        var result = Helpers.CurrentTimestamp();

        // Assert
        result.ShouldBeGreaterThan(minExpected);
    }

    /// <summary>
    /// Tests DictionaryToQueryString creates proper query string.
    /// </summary>
    [Fact]
    public void DictionaryToQueryString_CreatesValidQueryString()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            { "method", "track.scrobble" },
            { "artist", "Test Artist" }
        };

        // Act
        var result = Helpers.DictionaryToQueryString(data);

        // Assert
        result.ShouldContain("method=track.scrobble");
        result.ShouldContain("artist=Test%20Artist");
        result.ShouldContain("&");
    }

    /// <summary>
    /// Tests that empty/whitespace values are filtered out.
    /// </summary>
    [Fact]
    public void DictionaryToQueryString_FiltersEmptyValues()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            { "method", "test" },
            { "empty", "" },
            { "whitespace", "  " }
        };

        // Act
        var result = Helpers.DictionaryToQueryString(data);

        // Assert
        result.ShouldNotContain("empty=");
        result.ShouldNotContain("whitespace=");
        result.ShouldContain("method=test");
    }
}
