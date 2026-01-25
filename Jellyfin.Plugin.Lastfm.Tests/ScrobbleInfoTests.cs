// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Tests;

using Lastfm.Models;
using Xunit;

/// <summary>
/// Tests for ScrobbleInfo model.
/// </summary>
public class ScrobbleInfoTests
{
    [Fact]
    public void ScrobbleInfo_RequiredProperties_MustBeSet()
    {
        // Act
        var scrobble = new ScrobbleInfo
        {
            Artist = "Test Artist",
            Track = "Test Track",
            Timestamp = 1706198400
        };

        // Assert - required properties are set
        Assert.Equal("Test Artist", scrobble.Artist);
        Assert.Equal("Test Track", scrobble.Track);
        Assert.Equal(1706198400, scrobble.Timestamp);
    }

    [Fact]
    public void ScrobbleInfo_OptionalProperties_DefaultToNull()
    {
        // Arrange & Act
        var scrobble = new ScrobbleInfo
        {
            Artist = "Artist",
            Track = "Track",
            Timestamp = 123
        };

        // Assert - optional properties default to null
        Assert.Null(scrobble.Album);
        Assert.Null(scrobble.AlbumArtist);
        Assert.Null(scrobble.MusicBrainzId);
        Assert.Null(scrobble.Duration);
    }

    [Fact]
    public void ScrobbleInfo_WithAllValues_StoresCorrectly()
    {
        // Arrange & Act
        var scrobble = new ScrobbleInfo
        {
            Artist = "Test Artist",
            Track = "Test Track",
            Album = "Test Album",
            AlbumArtist = "Test Album Artist",
            MusicBrainzId = "mbid-123",
            Duration = 180,
            Timestamp = 1706198400
        };

        // Assert
        Assert.Equal("Test Artist", scrobble.Artist);
        Assert.Equal("Test Track", scrobble.Track);
        Assert.Equal("Test Album", scrobble.Album);
        Assert.Equal("Test Album Artist", scrobble.AlbumArtist);
        Assert.Equal("mbid-123", scrobble.MusicBrainzId);
        Assert.Equal(180, scrobble.Duration);
        Assert.Equal(1706198400, scrobble.Timestamp);
    }
}
