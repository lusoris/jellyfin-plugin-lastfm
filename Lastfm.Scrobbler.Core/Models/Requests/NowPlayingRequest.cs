// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Models.Requests
{
    public class NowPlayingRequest : BaseRequest
    {
        public override string Method => "track.updateNowPlaying";
        public string? Artist { get; set; }
        public string? Track { get; set; }
        public string? Album { get; set; }
        public string? SessionKey { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return ToDictionary(this);
        }
    }
}
