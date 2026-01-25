// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Services;

using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

/// <summary>
/// Matches Jellyfin tracks to Last.fm tracks.
/// </summary>
public partial class TrackMatcherService : ITrackMatcherService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TrackMatcherService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackMatcherService"/> class.
    /// </summary>
    public TrackMatcherService(ILibraryManager libraryManager, ILogger<TrackMatcherService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Audio?> FindMatchingTrackAsync(string lastfmArtist, string lastfmTrack, string? lastfmMbid, Guid userId)
    {
        // TODO: Implement track matching
        // 1. Try MusicBrainz ID if available
        // 2. Fall back to artist + track name fuzzy match
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool IsLike(string source, string target)
    {
        var normalizedSource = NormalizeString(source);
        var normalizedTarget = NormalizeString(target);

        return string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a string for comparison by removing special characters and whitespace.
    /// </summary>
    private static string NormalizeString(string input)
    {
        // Remove special characters, keep only alphanumeric
        var cleaned = SpecialCharsRegex().Replace(input, string.Empty);
        // Remove all whitespace
        return WhitespaceRegex().Replace(cleaned, string.Empty);
    }

    [GeneratedRegex(@"[\~#%&*{}/:<>?,\.\-\(\)\|""\']")]
    private static partial Regex SpecialCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
