// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Tests;

using Lastfm.Services;
using Xunit;

/// <summary>
/// Tests for the PlaylistResult model.
/// </summary>
public class PlaylistResultTests
{
    [Fact]
    public void SuccessResult_ReturnsSuccessfulResult()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        const string name = "Test Playlist";
        const int tracks = 25;

        // Act
        var result = PlaylistResult.SuccessResult(playlistId, name, tracks);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(playlistId, result.PlaylistId);
        Assert.Equal(name, result.PlaylistName);
        Assert.Equal(tracks, result.TracksAdded);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureResult_ReturnsFailedResult()
    {
        // Arrange
        const string error = "Something went wrong";

        // Act
        var result = PlaylistResult.FailureResult(error);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PlaylistId);
        Assert.Null(result.PlaylistName);
        Assert.Equal(0, result.TracksAdded);
        Assert.Equal(error, result.Error);
    }
}
