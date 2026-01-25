// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Xunit;
using Moq;
using Lastfm.Scrobbler.Core;
using Lastfm.Scrobbler.Core.Interfaces;

namespace Lastfm.Scrobbler.Core.Tests;

public class ScrobbleHandlerTests
{
    private readonly Mock<ILastfmApiClient> _apiClientMock;
    private readonly Mock<ICoreLogger<ScrobbleHandler>> _loggerMock;
    private readonly ScrobbleHandler _scrobbleHandler;

    public ScrobbleHandlerTests()
    {
        _apiClientMock = new Mock<ILastfmApiClient>();
        _loggerMock = new Mock<ICoreLogger<ScrobbleHandler>>();
        _scrobbleHandler = new ScrobbleHandler(_loggerMock.Object, _apiClientMock.Object);
    }

    [Fact]
    public void OnPlaybackStopped_MeetsAllConditions_ReturnsTrue()
    {
        // Arrange
        var playedTime = TimeSpan.FromMinutes(4).Ticks;
        var duration = TimeSpan.FromMinutes(5).Ticks;

        // Act
        var result = _scrobbleHandler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OnPlaybackStopped_TrackTooShort_ReturnsFalse()
    {
        // Arrange
        var playedTime = TimeSpan.FromSeconds(20).Ticks;
        var duration = TimeSpan.FromSeconds(29).Ticks;

        // Act
        var result = _scrobbleHandler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OnPlaybackStopped_NotPlayedLongEnough_ReturnsFalse()
    {
        // Arrange
        var playedTime = TimeSpan.FromMinutes(1).Ticks;
        var duration = TimeSpan.FromMinutes(10).Ticks;

        // Act
        var result = _scrobbleHandler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.False(result);
    }
}
