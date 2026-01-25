// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Queue;

using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>
/// JSON file-based queue for storing scrobbles when offline.
/// </summary>
public class ScrobbleQueue : IScrobbleQueue
{
    private readonly string _queueDirectory;
    private readonly ILogger<ScrobbleQueue> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrobbleQueue"/> class.
    /// </summary>
    public ScrobbleQueue(IApplicationPaths applicationPaths, ILogger<ScrobbleQueue> logger)
    {
        _queueDirectory = Path.Combine(applicationPaths.PluginConfigurationsPath, "lastfm", "queue");
        _logger = logger;

        Directory.CreateDirectory(_queueDirectory);
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(Guid userId, ScrobbleInfo scrobble)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var queue = await LoadQueueAsync(userId).ConfigureAwait(false);
            queue.Add(scrobble);
            await SaveQueueAsync(userId, queue).ConfigureAwait(false);

            _logger.LogDebug("Enqueued scrobble for user {UserId}: {Artist} - {Track}", userId, scrobble.Artist, scrobble.Track);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScrobbleInfo>> GetPendingAsync(Guid userId)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await LoadQueueAsync(userId).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DequeueAsync(Guid userId, int count)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var queue = await LoadQueueAsync(userId).ConfigureAwait(false);
            if (count >= queue.Count)
            {
                queue.Clear();
            }
            else
            {
                queue.RemoveRange(0, count);
            }

            await SaveQueueAsync(userId, queue).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> GetTotalPendingCountAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var total = 0;
            foreach (var file in Directory.GetFiles(_queueDirectory, "*.json"))
            {
                var json = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                var queue = JsonSerializer.Deserialize<List<ScrobbleInfo>>(json, JsonOptions);
                total += queue?.Count ?? 0;
            }

            return total;
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetQueuePath(Guid userId) => Path.Combine(_queueDirectory, $"{userId}.json");

    private async Task<List<ScrobbleInfo>> LoadQueueAsync(Guid userId)
    {
        var path = GetQueuePath(userId);
        if (!File.Exists(path))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<ScrobbleInfo>>(json, JsonOptions) ?? [];
    }

    private async Task SaveQueueAsync(Guid userId, List<ScrobbleInfo> queue)
    {
        var path = GetQueuePath(userId);
        if (queue.Count == 0)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        var json = JsonSerializer.Serialize(queue, JsonOptions);
        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
    }
}
