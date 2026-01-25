// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Models.Requests
{
    public class TrackLoveRequest : BaseRequest
    {
        public override string Method => Love ? "track.love" : "track.unlove";
        public string? Track { get; set; }
        public string? Artist { get; set; }
        public string? SessionKey { get; set; }
        public bool Love { get; set; } = true;

        public override Dictionary<string, string> ToDictionary()
        {
            return ToDictionary(this);
        }
    }
}
