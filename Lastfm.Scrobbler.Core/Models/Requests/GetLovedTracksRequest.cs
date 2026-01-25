// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Models.Requests
{
    public class GetLovedTracksRequest : BaseRequest
    {
        public override string Method => "user.getlovedtracks";
        public string? User { get; set; }
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 50;

        public override Dictionary<string, string> ToDictionary()
        {
            return ToDictionary(this);
        }
    }
}
