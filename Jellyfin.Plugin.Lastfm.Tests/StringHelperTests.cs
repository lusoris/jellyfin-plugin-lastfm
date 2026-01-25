namespace Jellyfin.Plugin.Lastfm.Tests;

using Jellyfin.Plugin.Lastfm.Utils;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for the StringHelper utility class.
/// Used for comparing track/artist names with fuzzy matching.
/// </summary>
public class StringHelperTests
{
    /// <summary>
    /// Tests that identical strings are considered "like" each other.
    /// </summary>
    [Fact]
    public void IsLike_IdenticalStrings_ReturnsTrue()
    {
        // Act & Assert
        StringHelper.IsLike("Test Artist", "Test Artist").ShouldBeTrue();
    }

    /// <summary>
    /// Tests case-insensitive matching.
    /// </summary>
    [Theory]
    [InlineData("test", "TEST")]
    [InlineData("Test Artist", "test artist")]
    [InlineData("RADIOHEAD", "Radiohead")]
    public void IsLike_DifferentCase_ReturnsTrue(string source, string target)
    {
        // Act & Assert
        StringHelper.IsLike(source, target).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that special characters are ignored in comparison.
    /// This is important for matching track names that may have different punctuation.
    /// </summary>
    [Theory]
    [InlineData("Test-Artist", "Test Artist")]
    [InlineData("The (Song)", "The Song")]
    [InlineData("Track #1", "Track 1")]
    [InlineData("Song: Part 1", "Song Part 1")]
    [InlineData("Rock & Roll", "Rock  Roll")]
    public void IsLike_SpecialCharactersIgnored_ReturnsTrue(string source, string target)
    {
        // Act & Assert
        StringHelper.IsLike(source, target).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that whitespace differences are normalized.
    /// </summary>
    [Theory]
    [InlineData("Test  Artist", "TestArtist")]
    [InlineData("The   Song", "TheSong")]
    public void IsLike_WhitespaceDifferences_ReturnsTrue(string source, string target)
    {
        // Act & Assert
        StringHelper.IsLike(source, target).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that genuinely different strings are not matched.
    /// </summary>
    [Theory]
    [InlineData("Artist A", "Artist B")]
    [InlineData("Radiohead", "Coldplay")]
    [InlineData("Song", "Track")]
    public void IsLike_DifferentStrings_ReturnsFalse(string source, string target)
    {
        // Act & Assert
        StringHelper.IsLike(source, target).ShouldBeFalse();
    }
}
