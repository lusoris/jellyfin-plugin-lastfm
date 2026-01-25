// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Lastfm.Scrobbler.Core.Models.Responses;

namespace Lastfm.Scrobbler.Core.Models
{
    /// <summary>
    /// Paged tracks container for API responses.
    /// </summary>
    public class PagedTracks
    {
        [JsonPropertyName("track")]
        public List<LastfmTrack>? Track { get; set; }

        [JsonPropertyName("@attr")]
        public PaginationAttributes? Attributes { get; set; }
    }

    /// <summary>
    /// Base Last.fm track with common properties.
    /// </summary>
    public class BaseLastfmTrack
    {
        [JsonPropertyName("artist")]
        public LastfmArtist? Artist { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("mbid")]
        public string? Mbid { get; set; }
    }

    /// <summary>
    /// Artist information from Last.fm API.
    /// </summary>
    public class LastfmArtist
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("mbid")]
        public string? MusicBrainzId { get; set; }
    }

    /// <summary>
    /// Last.fm track with play count (from user.getRecentTracks, library.getTracks).
    /// </summary>
    public class LastfmTrack : BaseLastfmTrack
    {
        [JsonPropertyName("playcount")]
        public int PlayCount { get; set; }
    }
}
