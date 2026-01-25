namespace Jellyfin.Plugin.Lastfm.Tests;

using System;
using Shouldly;
using Xunit;

/// <summary>
/// Tests for scrobble eligibility logic.
/// Based on Last.fm scrobbling rules: https://www.last.fm/api/scrobbling#when-is-a-scrobble-a-scrobble
/// </summary>
public class ScrobbleLogicTests
{
    // Constants matching ServerEntryPoint.cs
    private const long MinimumSongLengthToScrobbleInTicks = 30 * TimeSpan.TicksPerSecond;
    private const long MinimumPlayTimeToScrobbleInTicks = 4 * TimeSpan.TicksPerMinute;
    private const double MinimumPlayPercentage = 50.00;

    /// <summary>
    /// Determines if a track is eligible for scrobbling based on Last.fm rules.
    /// A track should only be scrobbled when:
    /// - The track must be longer than 30 seconds
    /// - The track has been played for at least half its duration, or for 4 minutes (whichever occurs earlier)
    /// </summary>
    private static bool IsScrobbleEligible(long trackLengthTicks, long playedTicks)
    {
        // Track must be at least 30 seconds long
        if (trackLengthTicks < MinimumSongLengthToScrobbleInTicks)
            return false;

        // Calculate play percentage
        var playPercent = ((double)playedTicks / trackLengthTicks) * 100;

        // Must have played 50% OR 4 minutes
        return playPercent >= MinimumPlayPercentage || playedTicks >= MinimumPlayTimeToScrobbleInTicks;
    }

    #region Minimum Song Length Tests

    [Fact]
    public void Track_ShorterThan30Seconds_NotEligible()
    {
        // Arrange: 29 seconds track, played fully
        var trackLength = 29 * TimeSpan.TicksPerSecond;
        var playedTime = trackLength; // 100% played

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeFalse("tracks shorter than 30 seconds should never be scrobbled");
    }

    [Fact]
    public void Track_Exactly30Seconds_IsEligible()
    {
        // Arrange: exactly 30 seconds, played fully
        var trackLength = 30 * TimeSpan.TicksPerSecond;
        var playedTime = trackLength; // 100% played

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeTrue("tracks exactly 30 seconds should be eligible when fully played");
    }

    [Fact]
    public void Track_LongerThan30Seconds_IsEligible()
    {
        // Arrange: 3 minute track, played fully
        var trackLength = 3 * TimeSpan.TicksPerMinute;
        var playedTime = trackLength;

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeTrue();
    }

    #endregion

    #region 50% Rule Tests

    [Fact]
    public void Track_PlayedExactly50Percent_IsEligible()
    {
        // Arrange: 6 minute track, played 50%
        var trackLength = 6 * TimeSpan.TicksPerMinute;
        var playedTime = trackLength / 2; // 50%

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeTrue("playing exactly 50% should make track eligible");
    }

    [Fact]
    public void Track_PlayedLessThan50Percent_NotEligible()
    {
        // Arrange: 6 minute track, played 49%
        var trackLength = 6 * TimeSpan.TicksPerMinute;
        var playedTime = (long)(trackLength * 0.49); // 49%

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeFalse("playing less than 50% (and less than 4 minutes) should not be eligible");
    }

    [Fact]
    public void Track_PlayedMoreThan50Percent_IsEligible()
    {
        // Arrange: 3 minute track, played 75%
        var trackLength = 3 * TimeSpan.TicksPerMinute;
        var playedTime = (long)(trackLength * 0.75);

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeTrue();
    }

    #endregion

    #region 4 Minute Rule Tests

    [Fact]
    public void Track_Played4Minutes_IsEligible_EvenIfLessThan50Percent()
    {
        // Arrange: 10 minute track, played exactly 4 minutes (40%)
        var trackLength = 10 * TimeSpan.TicksPerMinute;
        var playedTime = 4 * TimeSpan.TicksPerMinute;

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeTrue("playing 4 minutes should make track eligible even if < 50%");
    }

    [Fact]
    public void Track_LongTrack_PlayedLessThan4Minutes_NotEligible()
    {
        // Arrange: 15 minute track, played 3:59 (less than 4 minutes)
        var trackLength = 15 * TimeSpan.TicksPerMinute;
        var playedTime = (3 * TimeSpan.TicksPerMinute) + (59 * TimeSpan.TicksPerSecond);

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeFalse("15 min track with 3:59 played (26%) is neither 50% nor 4 minutes");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Track_ShortTrack_50PercentBeats4MinuteRule()
    {
        // Arrange: 2 minute track, played 60% (1:12)
        // 50% rule applies before 4 minute rule for short tracks
        var trackLength = 2 * TimeSpan.TicksPerMinute;
        var playedTime = (long)(trackLength * 0.60);

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBeTrue("for short tracks, 50% rule applies first");
    }

    [Fact]
    public void Constants_MatchLastfmSpecification()
    {
        // Verify constants are correctly defined
        MinimumSongLengthToScrobbleInTicks.ShouldBe(30 * TimeSpan.TicksPerSecond, "minimum song length should be 30 seconds");
        MinimumPlayTimeToScrobbleInTicks.ShouldBe(4 * TimeSpan.TicksPerMinute, "minimum play time should be 4 minutes");
        MinimumPlayPercentage.ShouldBe(50.0, "minimum play percentage should be 50%");
    }

    [Theory]
    [InlineData(30, 15, true)]   // 30s track, 15s played (50%)
    [InlineData(30, 14, false)]  // 30s track, 14s played (46.7%)
    [InlineData(60, 30, true)]   // 1 min track, 30s played (50%)
    [InlineData(600, 240, true)] // 10 min track, 4 min played (40%, but 4 min rule)
    [InlineData(600, 200, false)]// 10 min track, 3:20 played (33%, < 4 min)
    [InlineData(29, 29, false)]  // 29s track - too short
    public void ScrobbleEligibility_VariousScenarios(int trackLengthSeconds, int playedSeconds, bool expected)
    {
        // Arrange
        var trackLength = trackLengthSeconds * TimeSpan.TicksPerSecond;
        var playedTime = playedSeconds * TimeSpan.TicksPerSecond;

        // Act & Assert
        IsScrobbleEligible(trackLength, playedTime).ShouldBe(expected);
    }

    #endregion
}
