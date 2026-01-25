// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Lastfm.Configuration;
using Lastfm.Scrobbler.Core.Interfaces;
using Lastfm.Scrobbler.Core.Models;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

/// <summary>
/// Scheduled task to process the offline scrobble queue.
/// </summary>
public sealed partial class ProcessScrobbleQueueTask : IScheduledTask
{
    private const int BatchSize = 50;

    [LoggerMessage(Level = LogLevel.Error, Message = "Plugin instance not initialized")]
    partial void LogPluginNotInitialized();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin not configured, skipping scrobble queue processing")]
    partial void LogPluginNotConfigured();

    [LoggerMessage(Level = LogLevel.Debug, Message = "No pending scrobbles in queue")]
    partial void LogQueueEmpty();

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} pending scrobbles")]
    partial void LogProcessingScrobbles(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing queue for user {User}")]
    partial void LogUserProcessingError(string user, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Queue processing complete: {Processed} scrobbles submitted")]
    partial void LogProcessingComplete(int processed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} pending scrobbles for {User}")]
    partial void LogProcessingUserScrobbles(int count, string user);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dequeued {Count} scrobbles for {User}")]
    partial void LogDequeuedScrobbles(int count, string user);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch scrobble failed for {User}")]
    partial void LogBatchScrobbleFailed(string user, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Submitted {Count} queued scrobbles for {User}")]
    partial void LogSubmittedScrobbles(int count, string user);

    private readonly IScrobbleQueue<Scrobble, Guid> _queue;
    private readonly ILastfmApiClient _apiClient;
    private readonly ILogger<ProcessScrobbleQueueTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessScrobbleQueueTask"/> class.
    /// </summary>
    public ProcessScrobbleQueueTask(
        IScrobbleQueue<Scrobble, Guid> queue,
        ILastfmApiClient apiClient,
        ILogger<ProcessScrobbleQueueTask> logger)
    {
        _queue = queue;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Process Last.fm Scrobble Queue";

    /// <inheritdoc />
    public string Key => "LastfmProcessQueue";

    /// <inheritdoc />
    public string Description => "Submits queued scrobbles that failed due to network issues";

    /// <inheritdoc />
    public string Category => "Last.fm";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run every 15 minutes
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromMinutes(15).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var totalPending = _queue.GetTotalPendingCount();
        if (totalPending == 0)
        {
            LogQueueEmpty();
            progress.Report(100);
            return;
        }

        LogProcessingScrobbles(totalPending);

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

        var usersToProcess = config.LastfmUsers
            .Where(u => u.HasValidSession && u.Options.ScrobbleEnabled)
            .ToList();

        var totalProcessed = 0;
        foreach (var userConfig in usersToProcess)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var processed = await ProcessUserQueueAsync(userConfig, cancellationToken).ConfigureAwait(false);
                totalProcessed += processed;
            }
            catch (Exception ex)
            {
                LogUserProcessingError(userConfig.Username, ex);
            }

            progress.Report((double)totalProcessed / totalPending * 100);
        }

        LogProcessingComplete(totalProcessed);
    }

    private async Task<int> ProcessUserQueueAsync(LastfmUser userConfig, CancellationToken cancellationToken)
    {
        var pending = _queue.GetPending(userConfig.JellyfinUserId).ToList();
        if (pending.Count == 0)
        {
            return 0;
        }

        LogProcessingUserScrobbles(pending.Count, userConfig.Username);

        var totalSubmitted = 0;

        // Process in batches of 50
        for (var i = 0; i < pending.Count; i += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchScrobbles = pending.Skip(i).Take(BatchSize).ToList();

            // Convert Scrobbles to Tracks
            var batchTracks = batchScrobbles.Select(s => new Track
            {
                Name = s.Track,
                Artists = new List<string> { s.Artist },
                Album = s.Album,
                AlbumArtists = string.IsNullOrEmpty(s.AlbumArtist) ? null : new List<string> { s.AlbumArtist },
                RunTimeTicks = s.Duration.HasValue ? TimeSpan.FromSeconds(s.Duration.Value).Ticks : (long?)null,
                Timestamp = s.Timestamp,
                ProviderIds = string.IsNullOrEmpty(s.MusicBrainzId) ? null : new Dictionary<string, string> { ["MusicBrainzTrackId"] = s.MusicBrainzId }
            }).ToList();

            // Submit batch
            try
            {
                await _apiClient.ScrobbleBatch(batchTracks, userConfig, cancellationToken)
                    .ConfigureAwait(false);

                // Success - update counters and remove from queue
                totalSubmitted += batchTracks.Count;
                _queue.Dequeue(userConfig.JellyfinUserId, batchTracks.Count);
                LogDequeuedScrobbles(batchTracks.Count, userConfig.Username);
            }
            catch (Exception ex)
            {
                // Stop processing on error (rate limit, auth failure, etc.)
                LogBatchScrobbleFailed(userConfig.Username, ex);
                break;
            }
        }

        LogSubmittedScrobbles(totalSubmitted, userConfig.Username);
        return totalSubmitted;
    }
}
