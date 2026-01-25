// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Handlers;

using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;

/// <summary>
/// Handles user data changes for syncing favorites to Last.fm loved tracks.
/// </summary>
public class UserDataEventHandler : IHostedService, IDisposable
{
    private readonly IUserDataManager _userDataManager;
    private readonly ILastfmApiClient _apiClient;
    private readonly ILogger<UserDataEventHandler> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataEventHandler"/> class.
    /// </summary>
    public UserDataEventHandler(
        IUserDataManager userDataManager,
        ILastfmApiClient apiClient,
        ILogger<UserDataEventHandler> logger)
    {
        _userDataManager = userDataManager;
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved += OnUserDataSaved;

        _logger.LogInformation("Last.fm user data event handler started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;

        _logger.LogInformation("Last.fm user data event handler stopped");
        return Task.CompletedTask;
    }

    private async void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        // TODO: Implement love/unlove sync when IsFavorite changes
        // Note: async void is correct for event handlers
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _userDataManager.UserDataSaved -= OnUserDataSaved;
            }

            _disposed = true;
        }
    }
}
