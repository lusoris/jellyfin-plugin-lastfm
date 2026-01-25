// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Plugin.Lastfm.Configuration;
using Jellyfin.Plugin.Lastfm.Models;
using Jellyfin.Plugin.Lastfm.Services;
using Lastfm.Scrobbler.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Lastfm.Api;

/// <summary>
/// REST API controller for Last.fm integration.
/// </summary>
[ApiController]
[Authorize]
[Route("Lastfm")]
[Produces(MediaTypeNames.Application.Json)]
public sealed partial class LastfmController : ControllerBase
{
    private readonly ILastfmApiClient _lastfmApiClient;
    private readonly global::Lastfm.Scrobbler.Core.Interfaces.IScrobbleQueue<Scrobble, Guid> _scrobbleQueue;
    private readonly IPlaylistService _playlistService;
    private readonly ILogger<LastfmController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LastfmController"/> class.
    /// </summary>
    /// <param name="lastfmApiClient">Last.fm API client.</param>
    /// <param name="scrobbleQueue">Scrobble queue.</param>
    /// <param name="playlistService">Playlist service.</param>
    /// <param name="logger">Logger.</param>
    public LastfmController(
        ILastfmApiClient lastfmApiClient,
        global::Lastfm.Scrobbler.Core.Interfaces.IScrobbleQueue<Scrobble, Guid> scrobbleQueue,
        IPlaylistService playlistService,
        ILogger<LastfmController> logger)
    {
        _lastfmApiClient = lastfmApiClient;
        _scrobbleQueue = scrobbleQueue;
        _playlistService = playlistService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with Last.fm.
    /// </summary>
    /// <param name="request">Authentication request.</param>
    /// <returns>Authentication result.</returns>
    [HttpPost("Authenticate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResult>> Authenticate([FromBody] AuthenticationRequest request)
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return BadRequest(new AuthenticationResult { Success = false, Error = "Plugin not initialized" });
        }

        if (!plugin.Configuration.IsConfigured())
        {
            return BadRequest(new AuthenticationResult { Success = false, Error = "Plugin API credentials not configured" });
        }

        LogAuthenticating(request.JellyfinUserId, request.Username);

        var response = await _lastfmApiClient.GetMobileSessionAsync(request.Username, request.Password).ConfigureAwait(false);

        if (response?.Session == null)
        {
            var errorMessage = response?.Error?.Message ?? "Authentication failed";
            LogAuthenticationFailed(request.Username, errorMessage);

            return Unauthorized(new AuthenticationResult
            {
                Success = false,
                Error = errorMessage
            });
        }

        // Save the session
        SaveUserSession(request.JellyfinUserId, response.Session.Name, response.Session.Key);

        LogAuthenticationSuccess(response.Session.Name);

        return Ok(new AuthenticationResult
        {
            Success = true,
            Username = response.Session.Name
        });
    }

    /// <summary>
    /// Gets the Last.fm connection status for a user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>User status.</returns>
    [HttpGet("Status/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserStatus> GetStatus([FromRoute] Guid userId)
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return NotFound(new UserStatus { IsConnected = false });
        }

        var user = plugin.Configuration.GetUserConfig(userId);

        var pendingScrobbles = _scrobbleQueue.GetPending(userId);

        return Ok(new UserStatus
        {
            IsConnected = user?.HasValidSession ?? false,
            Username = user?.Username,
            ScrobbleEnabled = user?.Options.ScrobbleEnabled ?? false,
            NowPlayingEnabled = user?.Options.NowPlayingEnabled ?? false,
            SyncFavoritesToLoved = user?.Options.SyncFavoritesToLoved ?? false,
            PendingScrobbleCount = pendingScrobbles.Count()
        });
    }

    /// <summary>
    /// Disconnects a user from Last.fm.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>Result.</returns>
    [HttpPost("Disconnect/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DisconnectResult> Disconnect([FromRoute] Guid userId)
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return NotFound(new DisconnectResult { Success = false, Error = "Plugin not initialized" });
        }

        var user = plugin.Configuration.GetUserConfig(userId);
        if (user == null)
        {
            return NotFound(new DisconnectResult { Success = false, Error = "User not found" });
        }

        LogDisconnecting(userId, user.Username);

        // Clear the session
        user.SessionKey = string.Empty;
        user.Username = string.Empty;
        plugin.SaveConfiguration();

        return Ok(new DisconnectResult { Success = true });
    }

    /// <summary>
    /// Gets the pending scrobble queue for a user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>Queue information.</returns>
    [HttpGet("Queue/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueueStatus> GetQueue([FromRoute] Guid userId)
    {
        var pending = _scrobbleQueue.GetPending(userId);
        var totalPending = _scrobbleQueue.GetTotalPendingCount();

        return Ok(new QueueStatus
        {
            UserId = userId,
            PendingCount = pending.Count(),
            TotalPendingCount = totalPending,
            OldestScrobble = pending.Any() ? pending.Min(s => s.Timestamp) : null,
            NewestScrobble = pending.Any() ? pending.Max(s => s.Timestamp) : null
        });
    }

    /// <summary>
    /// Creates a playlist based on similar artists to the user's listening history.
    /// </summary>
    /// <param name="request">Playlist creation request.</param>
    /// <returns>Playlist creation result.</returns>
    [HttpPost("Playlists/SimilarArtists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaylistResult>> CreateSimilarArtistsPlaylist([FromBody] PlaylistRequest request)
    {
        LogCreatingSimilarArtistsPlaylist(request.UserId);

        var result = await _playlistService.CreateSimilarArtistsPlaylistAsync(
            request.UserId,
            request.PlaylistName ?? "Similar Artists Mix",
            request.MaxTracks ?? 50).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Creates a playlist based on tracks similar to the user's loved tracks.
    /// </summary>
    /// <param name="request">Playlist creation request.</param>
    /// <returns>Playlist creation result.</returns>
    [HttpPost("Playlists/SimilarTracks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaylistResult>> CreateSimilarTracksPlaylist([FromBody] PlaylistRequest request)
    {
        LogCreatingSimilarTracksPlaylist(request.UserId);

        var result = await _playlistService.CreateSimilarTracksPlaylistAsync(
            request.UserId,
            request.PlaylistName ?? "Similar Tracks Mix",
            request.MaxTracks ?? 50).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Creates a playlist to rediscover old favorites that haven't been played recently.
    /// </summary>
    /// <param name="request">Playlist creation request.</param>
    /// <returns>Playlist creation result.</returns>
    [HttpPost("Playlists/RediscoverFavorites")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaylistResult>> CreateRediscoverFavoritesPlaylist([FromBody] PlaylistRequest request)
    {
        LogCreatingRediscoverPlaylist(request.UserId);

        var result = await _playlistService.CreateRediscoverFavoritesPlaylistAsync(
            request.UserId,
            request.PlaylistName ?? "Rediscover Favorites",
            request.MaxTracks ?? 50).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Creates a weekly mixtape playlist based on recent listening.
    /// </summary>
    /// <param name="request">Playlist creation request.</param>
    /// <returns>Playlist creation result.</returns>
    [HttpPost("Playlists/WeeklyMixtape")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaylistResult>> CreateWeeklyMixtapePlaylist([FromBody] PlaylistRequest request)
    {
        LogCreatingWeeklyMixtape(request.UserId);

        var result = await _playlistService.CreateWeeklyMixtapeAsync(
            request.UserId,
            request.PlaylistName ?? "Weekly Mixtape",
            request.MaxTracks ?? 50).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Creates a tag discovery playlist based on user's favorite tags.
    /// </summary>
    /// <param name="request">Playlist creation request.</param>
    /// <returns>Playlist creation result.</returns>
    [HttpPost("Playlists/TagDiscovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaylistResult>> CreateTagDiscoveryPlaylist([FromBody] PlaylistRequest request)
    {
        LogCreatingTagDiscoveryPlaylist(request.UserId);

        var result = await _playlistService.CreateTagDiscoveryPlaylistAsync(
            request.UserId,
            request.PlaylistName ?? "Tag Discovery",
            request.MaxTracks ?? 50).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    private static void SaveUserSession(Guid jellyfinUserId, string username, string sessionKey)
    {
        var plugin = Plugin.Instance;
        if (plugin == null)
        {
            return;
        }

        var config = plugin.Configuration;
        var existingUser = config.GetUserConfig(jellyfinUserId);

        if (existingUser != null)
        {
            existingUser.Username = username;
            existingUser.SessionKey = sessionKey;
        }
        else
        {
            config.LastfmUsers.Add(new global::Lastfm.Scrobbler.Core.Models.LastfmUser
            {
                JellyfinUserId = jellyfinUserId,
                Username = username,
                SessionKey = sessionKey
            });
        }

        plugin.SaveConfiguration();
    }
}

/// <summary>
/// Authentication request.
/// </summary>
public class AuthenticationRequest
{
    /// <summary>
    /// Gets or sets the Jellyfin user ID.
    /// </summary>
    [Required]
    public Guid JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets the Last.fm username.
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Last.fm password.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Authentication result.
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether authentication was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the authenticated Last.fm username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the error message if authentication failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// User status response.
/// </summary>
public class UserStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether the user is connected to Last.fm.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the Last.fm username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether scrobbling is enabled.
    /// </summary>
    public bool ScrobbleEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether now playing is enabled.
    /// </summary>
    public bool NowPlayingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether favorites sync to loved.
    /// </summary>
    public bool SyncFavoritesToLoved { get; set; }

    /// <summary>
    /// Gets or sets the number of pending scrobbles in queue.
    /// </summary>
    public int PendingScrobbleCount { get; set; }
}

/// <summary>
/// Disconnect result.
/// </summary>
public class DisconnectResult
{
    /// <summary>
    /// Gets or sets a value indicating whether disconnection was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if disconnection failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Queue status response.
/// </summary>
public class QueueStatus
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the pending scrobble count for this user.
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Gets or sets the total pending scrobble count for all users.
    /// </summary>
    public int TotalPendingCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the oldest pending scrobble.
    /// </summary>
    public long? OldestScrobble { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the newest pending scrobble.
    /// </summary>
    public long? NewestScrobble { get; set; }
}

/// <summary>
/// Request to create a playlist.
/// </summary>
public class PlaylistRequest
{
    /// <summary>
    /// Gets or sets the Jellyfin user ID.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tracks.
    /// </summary>
    public int? MaxTracks { get; set; }
}
