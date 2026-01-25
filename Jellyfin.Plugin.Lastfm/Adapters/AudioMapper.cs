// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Adapters;

using System;
using global::Lastfm.Scrobbler.Abstractions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;

/// <summary>
/// Centralized Audio to DTO mapping to avoid code duplication.
/// </summary>
internal static class AudioMapper
{
    // Common strings for interning to reduce memory allocations
    private static readonly string UnknownArtist = string.Intern("Unknown Artist");
    private static readonly string UnknownAlbum = string.Intern("Unknown Album");

    /// <summary>
    /// Maps Jellyfin Audio entity to platform-agnostic MediaItemDto.
    /// Uses string interning for common values to reduce memory overhead.
    /// </summary>
    /// <param name="audio">The Jellyfin audio entity.</param>
    /// <returns>Platform-agnostic DTO.</returns>
    public static MediaItemDto MapToDto(Audio audio)
    {
        // Intern artist/album names for memory efficiency
        var artist = audio.Artists.Count > 0
            ? InternIfCommon(audio.Artists[0])
            : UnknownArtist;

        var album = !string.IsNullOrEmpty(audio.Album)
            ? InternIfCommon(audio.Album)
            : UnknownAlbum;

        var albumArtist = audio.AlbumArtists.Count > 0
            ? InternIfCommon(audio.AlbumArtists[0])
            : artist;

        return new MediaItemDto
        {
            Id = audio.Id,
            Name = audio.Name, // Don't intern - likely unique
            Artist = artist,
            Album = album,
            AlbumArtist = albumArtist,
            MusicBrainzRecordingId = audio.GetProviderId(MetadataProvider.MusicBrainzRecording),
            MusicBrainzTrackId = audio.GetProviderId(MetadataProvider.MusicBrainzTrack),
            RuntimeTicks = audio.RunTimeTicks
        };
    }

    /// <summary>
    /// Interns strings that appear frequently to reduce memory usage.
    /// Only interns if string is short enough to benefit.
    /// </summary>
    private static string InternIfCommon(string value)
    {
        // Only intern relatively short strings (artist/album names)
        // Very long strings in the intern pool can cause memory issues
        if (string.IsNullOrEmpty(value) || value.Length > 100)
        {
            return value;
        }

        return string.Intern(value);
    }
}
