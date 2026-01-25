// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

using Configuration;
using MediaBrowser.Controller.Library;
using Models;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Services;

/// <summary>
/// Scheduled task to sync play counts from Last.fm to Jellyfin.
/// </summary>
public sealed partial class SyncPlayCountsTask : IScheduledTask
{
    private readonly ILastfmApiClient _apiClient;
    private readonly ITrackMatcherService _trackMatcher;
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<SyncPlayCountsTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayCountsTask"/> class.
    /// </summary>
    public SyncPlayCountsTask(
        ILastfmApiClient apiClient,
        ITrackMatcherService trackMatcher,
        IUserDataManager userDataManager,
        IUserManager userManager,
        ILogger<SyncPlayCountsTask> logger)
    {
        _apiClient = apiClient;
        _trackMatcher = trackMatcher;
        _userDataManager = userDataManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Sync Last.fm Play Counts";

    /// <inheritdoc />
    public string Key => "LastfmSyncPlayCounts";

    /// <inheritdoc />
    public string Description => "Imports play counts from Last.fm and updates Jellyfin library";

    /// <inheritdoc />
    public string Category => "Last.fm";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run weekly at 4 AM on Sunday
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        LogStartingPlayCountsSync();

        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
        {
            LogPluginNotConfigured();
            progress.Report(100);
            return;
        }

        var usersToSync = config.LastfmUsers
            .Where(u => u.HasValidSession && u.Options.ImportPlayCounts)
            .ToList();

        if (usersToSync.Count == 0)
        {
            LogNoUsersConfigured();
            progress.Report(100);
            return;
        }

        var processedUsers = 0;
        foreach (var userConfig in usersToSync)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await SyncUserPlayCountsAsync(userConfig, config.DefaultPlayCountStrategy, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogSyncError(ex, userConfig.Username);
            }

            processedUsers++;
            progress.Report((double)processedUsers / usersToSync.Count * 100);
        }

        LogCompletedPlayCountsSync();
    }

    private async Task SyncUserPlayCountsAsync(
        LastfmUser userConfig,
        PlayCountSyncStrategy strategy,
        CancellationToken cancellationToken)
    {
        LogSyncingUser(userConfig.Username, strategy);

        var jellyfinUser = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (jellyfinUser == null)
        {
            LogUserNotFound(userConfig.JellyfinUserId);
            return;
        }

        var page = 1;
        var totalUpdated = 0;
        var totalProcessed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _apiClient.GetTopTracksAsync(
                userConfig.Username,
                "overall",
                page,
                1000,
                cancellationToken).ConfigureAwait(false);

            if (response == null || !response.HasTracks)
            {
                break;
            }

            var tracks = response.TopTracks!.Tracks!;
            foreach (var topTrack in tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalProcessed++;

                var artistName = topTrack.Artist?.Name;
                if (string.IsNullOrEmpty(artistName) || topTrack.PlayCount <= 0)
                {
                    continue;
                }

                var match = await _trackMatcher.FindMatchingTrackAsync(
                    artistName,
                    topTrack.Name,
                    topTrack.MusicBrainzId,
                    userConfig.JellyfinUserId).ConfigureAwait(false);

                if (match == null)
                {
                    continue;
                }

                // Get current user data
                var userData = _userDataManager.GetUserData(jellyfinUser, match);
                if (userData == null)
                {
                    continue;
                }

                var newPlayCount = CalculateNewPlayCount(
                    userData.PlayCount,
                    topTrack.PlayCount,
                    strategy);

                if (newPlayCount != userData.PlayCount)
                {
                    var oldCount = userData.PlayCount;
                    userData.PlayCount = newPlayCount;
                    userData.Played = newPlayCount > 0;
                    _userDataManager.SaveUserData(jellyfinUser, match, userData, UserDataSaveReason.Import, CancellationToken.None);
                    totalUpdated++;

                    LogUpdatedPlayCount(artistName, topTrack.Name, oldCount, newPlayCount);
                }
            }

            // Check if there are more pages
            var attrs = response.TopTracks!.Attributes;
            if (attrs == null || page >= attrs.TotalPages)
            {
                break;
            }

            page++;

            // Limit to first 5000 tracks to avoid excessive API calls
            if (totalProcessed >= 5000)
            {
                LogMaxTracksReached();
                break;
            }
        }

        // Update sync time
        userConfig.Options.LastPlayCountSyncTime = DateTime.UtcNow;

        LogSyncedPlayCounts(userConfig.Username, totalUpdated, totalProcessed);
    }

    private static int CalculateNewPlayCount(int jellyfinCount, int lastfmCount, PlayCountSyncStrategy strategy)
    {
        return strategy switch
        {
            PlayCountSyncStrategy.Add => jellyfinCount + lastfmCount,
            PlayCountSyncStrategy.Replace => lastfmCount,
            PlayCountSyncStrategy.Max => Math.Max(jellyfinCount, lastfmCount),
            _ => lastfmCount
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Last.fm play counts sync")]
    private partial void LogStartingPlayCountsSync();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin not configured, skipping play counts sync")]
    private partial void LogPluginNotConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "No users configured for play counts import")]
    private partial void LogNoUsersConfigured();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error syncing play counts for user {User}")]
    private partial void LogSyncError(Exception ex, string user);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed Last.fm play counts sync")]
    private partial void LogCompletedPlayCountsSync();

    [LoggerMessage(Level = LogLevel.Information, Message = "Syncing play counts for user {User} with strategy {Strategy}")]
    private partial void LogSyncingUser(string user, PlayCountSyncStrategy strategy);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Jellyfin user {UserId} not found")]
    private partial void LogUserNotFound(Guid userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Updated play count for {Artist} - {Track}: {OldCount} -> {NewCount}")]
    private partial void LogUpdatedPlayCount(string artist, string track, int oldCount, int newCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reached max tracks limit (5000), stopping pagination")]
    private partial void LogMaxTracksReached();

    [LoggerMessage(Level = LogLevel.Information, Message = "Synced play counts for {User}: {Updated} updated out of {Total} processed")]
    private partial void LogSyncedPlayCounts(string user, int updated, int total);
}
