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
/// Jellyfin implementation of media server adapter.
/// </summary>
public sealed partial class JellyfinMediaServerAdapter : IMediaServerAdapter
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<JellyfinMediaServerAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMediaServerAdapter"/> class.
    /// </summary>
    public JellyfinMediaServerAdapter(
        ILibraryManager libraryManager,
        IUserManager userManager,
        ILogger<JellyfinMediaServerAdapter> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ServerType => "Jellyfin";

    /// <inheritdoc />
    public Task<MediaItemDto?> FindTrackAsync(
        string artist,
        string track,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return Task.FromResult<MediaItemDto?>(null);
        }

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true,
            DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions(true)
        };

        var items = _libraryManager.GetItemList(query);

        var matchedItem = items
            .OfType<Audio>()
            .FirstOrDefault(a =>
                string.Equals(a.Name, track, StringComparison.OrdinalIgnoreCase) &&
                (a.AlbumArtists.Any(aa => string.Equals(aa, artist, StringComparison.OrdinalIgnoreCase)) ||
                 a.Artists.Any(art => string.Equals(art, artist, StringComparison.OrdinalIgnoreCase))));

        if (matchedItem != null)
        {
            LogTrackFound(matchedItem.Name, artist);
        }
        else
        {
            LogTrackNotFound(track, artist);
        }

        return Task.FromResult(matchedItem != null ? AudioMapper.MapToDto(matchedItem) : null);
    }

    /// <inheritdoc />
    public Task<MediaItemDto?> FindTrackByMusicBrainzIdAsync(
        string musicBrainzId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogUserNotFound(userId);
            return Task.FromResult<MediaItemDto?>(null);
        }

        var query = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Audio],
            Recursive = true,
            HasAnyProviderId = new Dictionary<string, string>
            {
                [MetadataProvider.MusicBrainzRecording.ToString()] = musicBrainzId
            },
            DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions(true)
        };

        var items = _libraryManager.GetItemList(query);
        var matchedItem = items.OfType<Audio>().FirstOrDefault();

        return Task.FromResult(matchedItem != null ? AudioMapper.MapToDto(matchedItem) : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MediaItemDto>> GetAllTracksAsync(
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
            DtoOptions = new MediaBrowser.Controller.Dto.DtoOptions(true)
        };

        var items = _libraryManager.GetItemList(query);
        IReadOnlyList<MediaItemDto> tracks = items.OfType<Audio>().Select(AudioMapper.MapToDto).ToList();

        return Task.FromResult(tracks);
    }



    [LoggerMessage(Level = LogLevel.Debug, Message = "Found track: {TrackName} by {Artist}")]
    private partial void LogTrackFound(string trackName, string artist);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Track not found: {TrackName} by {Artist}")]
    private partial void LogTrackNotFound(string trackName, string artist);

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found")]
    private partial void LogUserNotFound(Guid userId);
}
