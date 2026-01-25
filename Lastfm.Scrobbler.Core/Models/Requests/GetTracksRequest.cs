// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Models.Requests
{
    public class GetTracksRequest : BaseRequest
    {
        public override string Method => "user.getrecenttracks";
        public string? User { get; set; }
        public int Limit { get; set; } = 200;
        public int Page { get; set; } = 1;
        public string? Artist { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return ToDictionary(this);
        }
    }
}
