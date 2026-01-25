// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Queue;
using Services;

/// <summary>
/// Scheduled task to process the offline scrobble queue.
/// </summary>
public class ProcessScrobbleQueueTask : IScheduledTask
{
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
                Type = TaskTriggerInfo.TriggerInterval,
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

        // TODO: Implement queue processing
        // 1. Get all configured users
        // 2. For each user, get pending scrobbles
        // 3. Submit in batches of 50
        // 4. Remove successful scrobbles from queue

        progress.Report(100);
    }
}
