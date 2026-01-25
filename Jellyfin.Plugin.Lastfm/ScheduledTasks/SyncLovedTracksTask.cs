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
/// Scheduled task to sync loved tracks from Last.fm to Jellyfin favorites.
/// </summary>
public class SyncLovedTracksTask : IScheduledTask
{
    private readonly ILastfmApiClient _apiClient;
    private readonly ITrackMatcherService _trackMatcher;
    private readonly IUserDataManager _userDataManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<SyncLovedTracksTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncLovedTracksTask"/> class.
    /// </summary>
    public SyncLovedTracksTask(
        ILastfmApiClient apiClient,
        ITrackMatcherService trackMatcher,
        IUserDataManager userDataManager,
        IUserManager userManager,
        ILogger<SyncLovedTracksTask> logger)
    {
        _apiClient = apiClient;
        _trackMatcher = trackMatcher;
        _userDataManager = userDataManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Import Last.fm Loved Tracks";

    /// <inheritdoc />
    public string Key => "LastfmSyncLovedTracks";

    /// <inheritdoc />
    public string Description => "Imports loved tracks from Last.fm and marks them as favorites in Jellyfin";

    /// <inheritdoc />
    public string Category => "Last.fm";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run daily at 3 AM
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Last.fm loved tracks sync");

        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
        {
            _logger.LogWarning("Plugin not configured, skipping loved tracks sync");
            progress.Report(100);
            return;
        }

        var usersToSync = config.LastfmUsers
            .Where(u => u.HasValidSession && u.Options.ImportLovedTracks)
            .ToList();

        if (usersToSync.Count == 0)
        {
            _logger.LogInformation("No users configured for loved tracks import");
            progress.Report(100);
            return;
        }

        var processedUsers = 0;
        foreach (var userConfig in usersToSync)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await SyncUserLovedTracksAsync(userConfig, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing loved tracks for user {User}", userConfig.Username);
            }

            processedUsers++;
            progress.Report((double)processedUsers / usersToSync.Count * 100);
        }

        _logger.LogInformation("Completed Last.fm loved tracks sync");
    }

    private async Task SyncUserLovedTracksAsync(LastfmUser userConfig, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing loved tracks for user {User}", userConfig.Username);

        var jellyfinUser = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (jellyfinUser == null)
        {
            _logger.LogWarning("Jellyfin user {UserId} not found", userConfig.JellyfinUserId);
            return;
        }

        var page = 1;
        var totalMatched = 0;
        var totalProcessed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _apiClient.GetLovedTracksAsync(userConfig.Username, page, 200, cancellationToken)
                .ConfigureAwait(false);

            if (response == null || !response.HasTracks)
            {
                break;
            }

            var tracks = response.LovedTracks!.Tracks!;
            foreach (var lovedTrack in tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totalProcessed++;

                var artistName = lovedTrack.Artist?.Name;
                if (string.IsNullOrEmpty(artistName))
                {
                    continue;
                }

                var match = await _trackMatcher.FindMatchingTrackAsync(
                    artistName,
                    lovedTrack.Name,
                    lovedTrack.MusicBrainzId,
                    userConfig.JellyfinUserId).ConfigureAwait(false);

                if (match == null)
                {
                    continue;
                }

                // Get current user data
                var userData = _userDataManager.GetUserData(jellyfinUser, match);
                if (userData != null && !userData.IsFavorite)
                {
                    userData.IsFavorite = true;
                    _userDataManager.SaveUserData(jellyfinUser, match, userData, UserDataSaveReason.Import, CancellationToken.None);
                    totalMatched++;
                    _logger.LogDebug("Marked as favorite: {Artist} - {Track}", artistName, lovedTrack.Name);
                }
            }

            // Check if there are more pages
            var attrs = response.LovedTracks!.Attributes;
            if (attrs == null || page >= attrs.TotalPages)
            {
                break;
            }

            page++;
        }

        // Update sync time
        userConfig.Options.LastLovedTracksSyncTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Synced loved tracks for {User}: {Matched} matched out of {Total} processed",
            userConfig.Username,
            totalMatched,
            totalProcessed);
    }
}
