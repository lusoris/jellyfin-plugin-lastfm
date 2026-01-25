// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Tests;

using Lastfm.Models;
using Xunit;

/// <summary>
/// Tests for the ScrobbleInfo model.
/// </summary>
public class ScrobbleInfoTests2
{
    [Fact]
    public void ScrobbleInfo_Constructor_SetsRequiredProperties()
    {
        // Arrange & Act
        var scrobble = new ScrobbleInfo
        {
            Artist = "Test Artist",
            Track = "Test Track",
            Album = "Test Album",
            Timestamp = 1234567890
        };

        // Assert
        Assert.Equal("Test Artist", scrobble.Artist);
        Assert.Equal("Test Track", scrobble.Track);
        Assert.Equal("Test Album", scrobble.Album);
        Assert.Equal(1234567890, scrobble.Timestamp);
    }

    [Fact]
    public void ScrobbleInfo_OptionalProperties_AreNullByDefault()
    {
        // Arrange & Act
        var scrobble = new ScrobbleInfo
        {
            Artist = "Artist",
            Track = "Track",
            Timestamp = 123456
        };

        // Assert
        Assert.Null(scrobble.Album);
        Assert.Null(scrobble.AlbumArtist);
        Assert.Null(scrobble.MusicBrainzId);
        Assert.Null(scrobble.Duration);
    }

    [Fact]
    public void ScrobbleInfo_Duration_CanBeSet()
    {
        // Arrange
        var scrobble = new ScrobbleInfo
        {
            Artist = "Artist",
            Track = "Track",
            Timestamp = 123456,
            Duration = 300
        };

        // Assert
        Assert.Equal(300, scrobble.Duration);
    }

    [Fact]
    public void ScrobbleInfo_MusicBrainzId_CanBeSet()
    {
        // Arrange
        var mbid = "12345678-1234-1234-1234-123456789012";
        var scrobble = new ScrobbleInfo
        {
            Artist = "Artist",
            Track = "Track",
            Timestamp = 123456,
            MusicBrainzId = mbid
        };

        // Assert
        Assert.Equal(mbid, scrobble.MusicBrainzId);
    }
}
