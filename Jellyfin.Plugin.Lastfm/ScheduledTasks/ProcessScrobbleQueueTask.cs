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
public class ProcessScrobbleQueueTask : IScheduledTask
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
            _logger.LogDebug("No pending scrobbles in queue");
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Processing {Count} pending scrobbles", totalPending);

        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.IsConfigured)
        {
            _logger.LogWarning("Plugin not configured, skipping queue processing");
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
                _logger.LogError(ex, "Error processing queue for user {User}", userConfig.Username);
            }

            progress.Report((double)totalProcessed / totalPending * 100);
        }

        _logger.LogInformation("Queue processing complete: {Processed} scrobbles submitted", totalProcessed);
    }

    private async Task<int> ProcessUserQueueAsync(LastfmUser userConfig, CancellationToken cancellationToken)
    {
        var pending = await _queue.GetPendingAsync(userConfig.JellyfinUserId).ConfigureAwait(false);
        if (pending.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation("Processing {Count} pending scrobbles for {User}", pending.Count, userConfig.Username);

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
                    _logger.LogDebug("Dequeued {Count} scrobbles for {User}", batch.Count, userConfig.Username);
                }
            }
            else if (response?.IsError == true)
            {
                // Stop processing on error (rate limit, auth failure, etc.)
                _logger.LogWarning(
                    "Batch scrobble failed for {User}: {Error}",
                    userConfig.Username,
                    response.Error?.Message ?? "Unknown error");
                break;
            }
        }

        _logger.LogInformation("Submitted {Count} queued scrobbles for {User}", totalSubmitted, userConfig.Username);
        return totalSubmitted;
    }
}
