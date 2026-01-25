// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

using Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Models;
using Queue;
using Services;

/// <summary>
/// Scheduled task to process the offline scrobble queue.
/// </summary>
public sealed partial class ProcessScrobbleQueueTask : IScheduledTask
{
    private const int BatchSize = 50;

    private readonly IScrobbleQueue _queue;
    private readonly ILastfmApiClient _apiClient;
    private readonly ILogger<ProcessScrobbleQueueTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessScrobbleQueueTask"/> class.
    /// </summary>
    public ProcessScrobbleQueueTask(
        IScrobbleQueue queue,
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
        var totalPending = await _queue.GetTotalPendingCountAsync().ConfigureAwait(false);
        if (totalPending == 0)
        {
            LogNoPending();
            progress.Report(100);
            return;
        }

        LogProcessingPending(totalPending);

        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
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
                LogProcessingError(ex, userConfig.Username);
            }

            progress.Report((double)totalProcessed / totalPending * 100);
        }

        LogProcessingComplete(totalProcessed);
    }

    private async Task<int> ProcessUserQueueAsync(LastfmUser userConfig, CancellationToken cancellationToken)
    {
        var pending = await _queue.GetPendingAsync(userConfig.JellyfinUserId).ConfigureAwait(false);
        if (pending.Count == 0)
        {
            return 0;
        }

        LogProcessingUser(pending.Count, userConfig.Username);

        var totalSubmitted = 0;

        // Process in batches of 50
        for (var i = 0; i < pending.Count; i += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = pending.Skip(i).Take(BatchSize).ToList();
            var response = await _apiClient.ScrobbleBatchAsync(batch, userConfig.SessionKey, cancellationToken)
                .ConfigureAwait(false);

            if (response?.Scrobbles?.Attributes != null)
            {
                var accepted = response.Scrobbles.Attributes.Accepted;
                totalSubmitted += accepted;

                // Remove successfully submitted scrobbles from queue
                if (accepted > 0)
                {
                    await _queue.DequeueAsync(userConfig.JellyfinUserId, batch.Count).ConfigureAwait(false);
                    LogDequeued(batch.Count, userConfig.Username);
                }
            }
            else if (response?.IsError == true)
            {
                // Stop processing on error (rate limit, auth failure, etc.)
                LogBatchFailed(
                    userConfig.Username,
                    response.Error?.Message ?? "Unknown error");
                break;
            }
        }

        LogSubmittedQueuedScrobbles(totalSubmitted, userConfig.Username);
        return totalSubmitted;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No pending scrobbles in queue")]
    private partial void LogNoPending();

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} pending scrobbles")]
    private partial void LogProcessingPending(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Plugin not configured, skipping queue processing")]
    private partial void LogPluginNotConfigured();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing queue for user {User}")]
    private partial void LogProcessingError(Exception ex, string user);

    [LoggerMessage(Level = LogLevel.Information, Message = "Queue processing complete: {Processed} scrobbles submitted")]
    private partial void LogProcessingComplete(int processed);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing {Count} pending scrobbles for {User}")]
    private partial void LogProcessingUser(int count, string user);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dequeued {Count} scrobbles for {User}")]
    private partial void LogDequeued(int count, string user);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch scrobble failed for {User}: {Error}")]
    private partial void LogBatchFailed(string user, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Submitted {Count} queued scrobbles for {User}")]
    private partial void LogSubmittedQueuedScrobbles(int count, string user);
}
