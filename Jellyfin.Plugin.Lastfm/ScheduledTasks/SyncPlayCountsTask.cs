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
public class SyncPlayCountsTask : IScheduledTask
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
        _logger.LogInformation("Starting Last.fm play counts sync");

        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
        {
            _logger.LogWarning("Plugin not configured, skipping play counts sync");
            progress.Report(100);
            return;
        }

        var usersToSync = config.LastfmUsers
            .Where(u => u.HasValidSession && u.Options.ImportPlayCounts)
            .ToList();

        if (usersToSync.Count == 0)
        {
            _logger.LogInformation("No users configured for play counts import");
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
                _logger.LogError(ex, "Error syncing play counts for user {User}", userConfig.Username);
            }

            processedUsers++;
            progress.Report((double)processedUsers / usersToSync.Count * 100);
        }

        _logger.LogInformation("Completed Last.fm play counts sync");
    }

    private async Task SyncUserPlayCountsAsync(
        LastfmUser userConfig,
        PlayCountSyncStrategy strategy,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing play counts for user {User} with strategy {Strategy}",
            userConfig.Username, strategy);

        var jellyfinUser = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (jellyfinUser == null)
        {
            _logger.LogWarning("Jellyfin user {UserId} not found", userConfig.JellyfinUserId);
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
                    userData.PlayCount = newPlayCount;
                    userData.Played = newPlayCount > 0;
                    _userDataManager.SaveUserData(jellyfinUser, match, userData, UserDataSaveReason.Import, CancellationToken.None);
                    totalUpdated++;

                    _logger.LogDebug(
                        "Updated play count for {Artist} - {Track}: {OldCount} -> {NewCount}",
                        artistName,
                        topTrack.Name,
                        userData.PlayCount,
                        newPlayCount);
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
                _logger.LogInformation("Reached max tracks limit (5000), stopping pagination");
                break;
            }
        }

        // Update sync time
        userConfig.Options.LastPlayCountSyncTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Synced play counts for {User}: {Updated} updated out of {Total} processed",
            userConfig.Username,
            totalUpdated,
            totalProcessed);
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
}
