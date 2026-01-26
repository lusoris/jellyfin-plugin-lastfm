// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using Xunit;
using Moq;
using Lastfm.Scrobbler.Core;
using Lastfm.Scrobbler.Core.Interfaces;

namespace Lastfm.Scrobbler.Core.Tests;

public class ScrobbleHandlerTests
{
    private ScrobbleHandler CreateScrobbleHandler()
    {
        var apiClientMock = new Mock<ILastfmApiClient>();
        var loggerMock = new Mock<ICoreLogger<ScrobbleHandler>>();
        return new ScrobbleHandler(loggerMock.Object, apiClientMock.Object);
    }

    [Fact]
    public void IsScrobbleEligible_PlayedOver4Minutes_ReturnsTrue()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        var playedTime = TimeSpan.FromMinutes(4).Ticks;
        var duration = TimeSpan.FromMinutes(5).Ticks;

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsScrobbleEligible_TrackTooShort_ReturnsFalse()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        var playedTime = TimeSpan.FromSeconds(20).Ticks;
        var duration = TimeSpan.FromSeconds(29).Ticks; // < 30 seconds

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsScrobbleEligible_NotPlayedLongEnough_ReturnsFalse()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        var playedTime = TimeSpan.FromMinutes(1).Ticks; // Only 10%
        var duration = TimeSpan.FromMinutes(10).Ticks;

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.False(result); // < 4min AND < 50%
    }

    [Fact]
    public void IsScrobbleEligible_PlayedHalfOfLongTrack_ReturnsTrue()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        var duration = TimeSpan.FromMinutes(8).Ticks;
        var playedTime = TimeSpan.FromMinutes(4).Ticks; // Exactly 50%

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.True(result); // 50% of 8 minutes
    }

    [Fact]
    public void IsScrobbleEligible_Played240Seconds_ReturnsTrue()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        var duration = TimeSpan.FromMinutes(20).Ticks; // 20 min track
        var playedTime = TimeSpan.FromSeconds(240).Ticks; // Exactly 4 minutes

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.True(result); // 4 min threshold met
    }

    [Fact]
    public void IsScrobbleEligible_NullDuration_ReturnsFalse()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        long? duration = null;
        long? playedTime = TimeSpan.FromMinutes(5).Ticks;

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsScrobbleEligible_NullPlayedTime_ReturnsFalse()
    {
        // Arrange
        var handler = CreateScrobbleHandler();
        long? duration = TimeSpan.FromMinutes(5).Ticks;
        long? playedTime = null;

        // Act
        var result = handler.IsScrobbleEligible(duration, playedTime);

        // Assert
        Assert.False(result);
    }
}
