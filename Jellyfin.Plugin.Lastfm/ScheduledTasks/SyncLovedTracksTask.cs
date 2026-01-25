// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.ScheduledTasks;

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
    private readonly ILogger<SyncLovedTracksTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncLovedTracksTask"/> class.
    /// </summary>
    public SyncLovedTracksTask(
        ILastfmApiClient apiClient,
        ITrackMatcherService trackMatcher,
        ILogger<SyncLovedTracksTask> logger)
    {
        _apiClient = apiClient;
        _trackMatcher = trackMatcher;
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

        // TODO: Implement loved tracks sync
        // 1. Get all configured users
        // 2. For each user, fetch loved tracks from Last.fm
        // 3. Match to Jellyfin library
        // 4. Mark as favorite

        progress.Report(100);
        _logger.LogInformation("Completed Last.fm loved tracks sync");
    }
}
