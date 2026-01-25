// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Lastfm.Configuration;
using Jellyfin.Plugin.Lastfm.Interfaces;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using PlayCountSyncStrategy = Jellyfin.Plugin.Lastfm.Configuration.PlayCountSyncStrategy;

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

/// <summary>
/// Scheduled task to sync play counts from Last.fm to Jellyfin.
/// </summary>
public sealed partial class SyncPlayCountsTask : IScheduledTask
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Last.fm play counts sync")]
    partial void LogSyncStarting();

    [LoggerMessage(Level = LogLevel.Error, Message = "Plugin instance not initialized")]
    partial void LogPluginNotInitialized();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin not configured, skipping play counts sync")]
    partial void LogPluginNotConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "No users configured for play counts import")]
    partial void LogNoUsersConfigured();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error syncing play counts for user {User}")]
    partial void LogUserSyncError(string user, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed Last.fm play counts sync")]
    partial void LogSyncCompleted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Syncing play counts for user {User} with strategy {Strategy}")]
    partial void LogUserSyncStart(string user, PlayCountSyncStrategy strategy);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Jellyfin user {UserId} not found")]
    partial void LogJellyfinUserNotFound(Guid userId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Updated play count for {Artist} - {Track}: {OldCount} -> {NewCount}")]
    partial void LogPlayCountUpdated(string artist, string track, int oldCount, int newCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Synced play counts for {User}: {Updated} updated out of {Total} processed")]
    partial void LogUserSyncCompleted(string user, int updated, int total);
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
        LogSyncStarting();

        if (Plugin.Instance == null)
        {
            LogPluginNotInitialized();
            return;
        }

        var config = Plugin.Instance.Configuration;
        if (!config.IsConfigured())
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
                LogUserSyncError(userConfig.Username, ex);
            }

            processedUsers++;
            progress.Report((double)processedUsers / usersToSync.Count * 100);
        }

        LogSyncCompleted();
    }

    private async Task SyncUserPlayCountsAsync(
        LastfmUser userConfig,
        PlayCountSyncStrategy strategy,
        CancellationToken cancellationToken)
    {
        LogUserSyncStart(userConfig.Username, strategy);

        var jellyfinUser = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (jellyfinUser == null)
        {
            LogJellyfinUserNotFound(userConfig.JellyfinUserId);
            return;
        }

        var page = 1;
        var totalUpdated = 0;
        var totalProcessed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _apiClient.GetRecentTracks(
                userConfig,
                page,
                1000,
                cancellationToken).ConfigureAwait(false);

            if (response == null || !response.HasTracks)
            {
                break;
            }

            var tracks = response.Tracks?.Track;
            if (tracks == null)
            {
                break;
            }

            foreach (var lastfmTrack in tracks)
            {
                totalProcessed++;
                var matchedTrack = await _trackMatcher.FindMatchingTrackAsync(
                    lastfmTrack.Artist?.Name ?? string.Empty,
                    lastfmTrack.Name,
                    lastfmTrack.Mbid,
                    jellyfinUser.Id).ConfigureAwait(false);

                if (matchedTrack == null)
                {
                    continue;
                }

                var userData = _userDataManager.GetUserData(jellyfinUser, matchedTrack);
                var currentPlayCount = userData!.PlayCount;
                var lastfmPlayCount = lastfmTrack.PlayCount;

                if (ShouldUpdatePlayCount(currentPlayCount, lastfmPlayCount, strategy))
                {
                    userData.PlayCount = lastfmPlayCount;
                    _userDataManager.SaveUserData(jellyfinUser, matchedTrack, userData, UserDataSaveReason.Import, CancellationToken.None);

                    var artistName = matchedTrack.Artists?.FirstOrDefault() ?? "Unknown";
                    LogPlayCountUpdated(artistName, matchedTrack.Name, currentPlayCount, lastfmPlayCount);

                    totalUpdated++;
                }
            }

            if (response.Tracks?.Attributes != null &&
                response.Tracks.Attributes.Page >= response.Tracks.Attributes.TotalPages)
            {
                break;
            }

            page++;
        }

        LogUserSyncCompleted(userConfig.Username, totalUpdated, totalProcessed);
    }

    private static bool ShouldUpdatePlayCount(int currentPlayCount, int lastfmPlayCount, PlayCountSyncStrategy strategy)
    {
        return strategy switch
        {
            PlayCountSyncStrategy.Replace => true, // Always replace
            PlayCountSyncStrategy.Max => lastfmPlayCount > currentPlayCount, // Only if Last.fm is higher
            PlayCountSyncStrategy.Add => lastfmPlayCount > 0, // Add if Last.fm has data
            _ => false
        };
    }
}
