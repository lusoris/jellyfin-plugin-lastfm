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
using Lastfm.Scrobbler.Core.Models.Responses;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

/// <summary>
/// Scheduled task to sync loved tracks from Last.fm to Jellyfin.
/// </summary>
public sealed partial class SyncLovedTracksTask : IScheduledTask
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Last.fm loved tracks sync")]
    partial void LogSyncStarting();

    [LoggerMessage(Level = LogLevel.Error, Message = "Plugin instance not initialized")]
    partial void LogPluginNotInitialized();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin not configured, skipping loved tracks sync")]
    partial void LogPluginNotConfigured();

    [LoggerMessage(Level = LogLevel.Information, Message = "No users configured for loved tracks sync")]
    partial void LogNoUsersConfigured();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error syncing loved tracks for user {User}")]
    partial void LogUserSyncError(string user, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed Last.fm loved tracks sync")]
    partial void LogSyncCompleted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Syncing loved tracks for user {User}")]
    partial void LogUserSyncStart(string user);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Jellyfin user {UserId} not found")]
    partial void LogJellyfinUserNotFound(Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Marked '{Track}' by '{Artist}' as a favorite for user {User}")]
    partial void LogTrackMarkedFavorite(string track, string artist, string user);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loved tracks sync complete for {User}: {Favorited} new tracks marked as favorite out of {Total} processed")]
    partial void LogUserSyncCompleted(string user, int favorited, int total);
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
    public string Name => "Sync Last.fm Loved Tracks";

    /// <inheritdoc />
    public string Key => "LastfmSyncLovedTracks";

    /// <inheritdoc />
    public string Description => "Fetches loved tracks from Last.fm and marks them as favorites in Jellyfin";

    /// <inheritdoc />
    public string Category => "Last.fm";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run weekly at 2 AM on Sunday
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(2).Ticks
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
            .Where(u => u.HasValidSession && u.Options.SyncLovedTracks)
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
                await SyncUserLovedTracksAsync(userConfig, cancellationToken).ConfigureAwait(false);
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

    private async Task SyncUserLovedTracksAsync(LastfmUser userConfig, CancellationToken cancellationToken)
    {
        LogUserSyncStart(userConfig.Username);

        var jellyfinUser = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (jellyfinUser == null)
        {
            LogJellyfinUserNotFound(userConfig.JellyfinUserId);
            return;
        }

        var page = 1;
        var totalFavorited = 0;
        var totalProcessed = 0;
        LovedTracksResponse? response = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            response = await _apiClient.GetLovedTracks(userConfig, page, 1000, cancellationToken)
                .ConfigureAwait(false);

            if (response == null || !response.HasTracks)
            {
                break;
            }

            var tracks = response.LovedTracks?.Tracks;
            if (tracks == null)
            {
                break;
            }

            foreach (var lovedTrack in tracks)
            {
                totalProcessed++;
                var matchedTrack = await _trackMatcher.FindMatchingTrackAsync(
                    lovedTrack.Artist?.Name ?? string.Empty,
                    lovedTrack.Name,
                    lovedTrack.MusicBrainzId,
                    jellyfinUser.Id).ConfigureAwait(false);

                if (matchedTrack == null)
                {
                    continue;
                }

                var userData = _userDataManager.GetUserData(jellyfinUser, matchedTrack);
                if (!userData!.IsFavorite)
                {
                    userData.IsFavorite = true;
                    _userDataManager.SaveUserData(jellyfinUser, matchedTrack, userData, UserDataSaveReason.Import, CancellationToken.None);

                    var artistName = matchedTrack.Artists?.FirstOrDefault() ?? "Unknown";
                    LogTrackMarkedFavorite(matchedTrack.Name, artistName, jellyfinUser.Username);

                    totalFavorited++;
                }
            }

            if (response.LovedTracks?.Attributes != null &&
                response.LovedTracks.Attributes.Page >= response.LovedTracks.Attributes.TotalPages)
            {
                break;
            }

            page++;
        }

        var totalPages = response?.LovedTracks?.Attributes?.TotalPages ?? 0;
        LogUserSyncCompleted(userConfig.Username, totalFavorited, totalPages);
    }
}

