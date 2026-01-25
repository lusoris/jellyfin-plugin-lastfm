// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Text.Json.Serialization;

namespace Lastfm.Scrobbler.Core.Models.Responses
{
    public class GetTracksResponse : BaseResponse
    {
        [JsonPropertyName("recenttracks")]
        public PagedTracks? Tracks { get; set; }

        /// <summary>
        /// Gets a value indicating whether there are any tracks.
        /// </summary>
        public bool HasTracks => Tracks?.Track?.Count > 0;
    }
}
