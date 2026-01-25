// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Text.Json.Serialization;
using Lastfm.Scrobbler.Core.Models;

namespace Lastfm.Scrobbler.Core.Models.Responses
{
    public class NowPlayingResponse : BaseResponse
    {
        [JsonPropertyName("nowplaying")]
        public Scrobble? NowPlaying { get; set; }
    }
}
