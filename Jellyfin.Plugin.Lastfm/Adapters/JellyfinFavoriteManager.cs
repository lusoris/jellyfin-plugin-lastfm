// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Adapters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::Lastfm.Scrobbler.Abstractions;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Jellyfin implementation of favorite manager.
/// </summary>
public sealed partial class JellyfinFavoriteManager : IFavoriteManager, IFavoriteEventProvider, IDisposable
{
    private readonly IUserDataManager _userDataManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<JellyfinFavoriteManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinFavoriteManager"/> class.
    /// </summary>
    public JellyfinFavoriteManager(
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        IUserManager userManager,
        ILogger<JellyfinFavoriteManager> logger)
    {
        _userDataManager = userDataManager;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<FavoriteChangedEventArgs>? FavoriteChanged;

    /// <summary>
    /// Starts listening to Jellyfin favorite change events.
    /// </summary>
    public void Start()
    {
        _userDataManager.UserDataSaved += OnUserDataSaved;
        LogEventListeningStarted();
    }

    /// <summary>
    /// Stops listening to Jellyfin favorite change events.
    /// </summary>
    public void Stop()
    {
        _userDataManager.UserDataSaved -= OnUserDataSaved;
        LogEventListeningStopped();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MediaItemDto>> GetFavoriteTracksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return Task.FromResult<IReadOnlyList<MediaItemDto>>(Array.Empty<MediaItemDto>());
        }

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true,
            IsFavorite = true,
            DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions(true)
        };

        var items = _libraryManager.GetItemList(query);
        IReadOnlyList<MediaItemDto> favorites = items.OfType<Audio>().Select(AudioMapper.MapToDto).ToList();

        return Task.FromResult(favorites);
    }

    /// <inheritdoc />
    public Task SetFavoriteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return Task.CompletedTask;
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            LogItemNotFound(itemId);
            return Task.CompletedTask;
        }

        var userData = _userDataManager.GetUserData(user, item);
        if (userData == null)
        {
            return Task.CompletedTask;
        }

        userData.IsFavorite = true;
        _userDataManager.SaveUserData(user, item, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnsetFavoriteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return Task.CompletedTask;
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            LogItemNotFound(itemId);
            return Task.CompletedTask;
        }

        var userData = _userDataManager.GetUserData(user, item);
        if (userData == null)
        {
            return Task.CompletedTask;
        }

        userData.IsFavorite = false;
        _userDataManager.SaveUserData(user, item, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);

        return Task.CompletedTask;
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        if (e.Item is not Audio audio)
        {
            return;
        }

        // Check if this is a favorite change
        if (e.SaveReason != UserDataSaveReason.UpdateUserRating)
        {
            return;
        }

        var eventArgs = new FavoriteChangedEventArgs
        {
            Item = AudioMapper.MapToDto(audio),
            UserId = e.UserId,
            IsFavorite = e.UserData.IsFavorite
        };

        FavoriteChanged?.Invoke(this, eventArgs);
    }



    [LoggerMessage(Level = LogLevel.Information, Message = "Started listening to Jellyfin favorite change events")]
    private partial void LogEventListeningStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped listening to Jellyfin favorite change events")]
    private partial void LogEventListeningStopped();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Item not found: {ItemId}")]
    private partial void LogItemNotFound(Guid itemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found")]
    private partial void LogUserNotFound(Guid userId);
}
