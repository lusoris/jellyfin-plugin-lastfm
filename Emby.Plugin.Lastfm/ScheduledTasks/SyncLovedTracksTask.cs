using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Entities;
using Lastfm.Scrobbler.Abstractions;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using MediaBrowser.Model.Querying;

namespace Emby.Plugin.Lastfm.ScheduledTasks;

/// <summary>
/// Scheduled task to sync loved tracks from Last.fm to Emby favorites.
/// Runs daily by default.
/// </summary>
public sealed class SyncLovedTracksTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILastfmApiClient _lastfmApiClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncLovedTracksTask"/> class.
    /// </summary>
    public SyncLovedTracksTask(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILastfmApiClient lastfmApiClient,
        ILogManager logManager)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _lastfmApiClient = lastfmApiClient;
        _logger = logManager.GetLogger(GetType().Name);
    }

    /// <inheritdoc/>
    public string Name => "Sync Loved Tracks from Last.fm";

    /// <inheritdoc/>
    public string Key => "LastfmSyncLovedTracks";

    /// <inheritdoc/>
    public string Description => "Import loved tracks from Last.fm and mark them as favorites in Emby";

    /// <inheritdoc/>
    public string Category => "Last.fm";

    /// <inheritdoc/>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        };
    }

    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        _logger.Info("Starting loved tracks sync from Last.fm");
        progress.Report(0);

        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                _logger.Warn("Plugin configuration not available");
                return;
            }

            var lastfmUsers = config.LastfmUsers;
            if (lastfmUsers == null || lastfmUsers.Length == 0)
            {
                _logger.Info("No Last.fm users configured");
                return;
            }

            var usersToSync = lastfmUsers.Where(u => u.Options.SyncLovedTracks).ToArray();
            if (usersToSync.Length == 0)
            {
                _logger.Info("No users enabled for loved tracks sync");
                return;
            }

            var totalUsers = usersToSync.Length;
            for (var i = 0; i < totalUsers; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Info("Sync cancelled");
                    break;
                }

                var user = usersToSync[i];
                _logger.Debug("Syncing loved tracks for user: {0}", user.Username);

                await SyncUserLovedTracksAsync(user, cancellationToken).ConfigureAwait(false);

                progress.Report((i + 1) * 100.0 / totalUsers);
            }

            _logger.Info("Loved tracks sync completed");
            progress.Report(100);
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error during loved tracks sync", ex);
            throw;
        }
    }

    private async Task SyncUserLovedTracksAsync(LastfmUser userConfig, CancellationToken cancellationToken)
    {
        _logger.Debug("Starting sync for Last.fm user: {0}", userConfig.Username);

        var embyUser = _userManager.GetUserById(userConfig.JellyfinUserId.ToString("N"));
        if (embyUser == null)
        {
            _logger.Warn("Emby user not found for Last.fm user: {0}", userConfig.Username);
            return;
        }

        var page = 1;
        var totalFavorited = 0;
        var totalProcessed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _lastfmApiClient.GetLovedTracks(userConfig, page, 1000, cancellationToken)
                .ConfigureAwait(false);

            if (response == null || !response.HasTracks)
            {
                break;
            }

            var tracks = response.LovedTracks?.Tracks;
            if (tracks == null || tracks.Count == 0)
            {
                break;
            }

            foreach (var lovedTrack in tracks)
            {
                totalProcessed++;

                var artistName = lovedTrack.Artist?.Name ?? string.Empty;
                var trackName = lovedTrack.Name ?? string.Empty;

                if (string.IsNullOrEmpty(artistName) || string.IsNullOrEmpty(trackName))
                {
                    continue;
                }

                var matchedTrack = FindMatchingTrack(artistName, trackName, lovedTrack.MusicBrainzId, embyUser);
                if (matchedTrack == null)
                {
                    _logger.Debug("No match found for: {0} - {1}", artistName, trackName);
                    continue;
                }

                var userData = _userDataManager.GetUserData(embyUser, matchedTrack);
                if (!userData.IsFavorite)
                {
                    userData.IsFavorite = true;
                    // Note: In Emby, user data changes are persisted automatically through the UserItemData object
                    // No explicit SaveUserData call needed
                    
                    _logger.Info("Marked as favorite: {0} - {1} for user {2}", artistName, trackName, embyUser.Name);
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

        _logger.Info("Sync completed for {0}: {1} tracks favorited out of {2} loved tracks",
            userConfig.Username, totalFavorited, totalProcessed);

        userConfig.Options.LastLovedTracksSyncTime = DateTime.UtcNow;
    }

    private Audio? FindMatchingTrack(string artist, string trackName, string? musicBrainzId, MediaBrowser.Controller.Entities.User user)
    {
        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = new[] { "Audio" },
            Recursive = true
        };

        var allTracks = _libraryManager.GetItemList(query).OfType<Audio>();

        // Try MusicBrainz ID match first
        if (!string.IsNullOrEmpty(musicBrainzId))
        {
            var mbidMatch = allTracks.FirstOrDefault(t =>
                t.ProviderIds != null &&
                t.ProviderIds.TryGetValue("MusicBrainzTrack", out var id) &&
                string.Equals(id, musicBrainzId, StringComparison.OrdinalIgnoreCase));

            if (mbidMatch != null)
            {
                return mbidMatch;
            }
        }

        // Fallback to name matching
        return allTracks.FirstOrDefault(t =>
            (t.Artists != null && t.Artists.Any(a => string.Equals(a, artist, StringComparison.OrdinalIgnoreCase))) &&
            string.Equals(t.Name, trackName, StringComparison.OrdinalIgnoreCase));
    }
}

