// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;

namespace Lastfm.Scrobbler.Core.Models.Requests
{
    public class MobileSessionRequest : BaseRequest
    {
        public override string Method => "auth.getmobilesession";
        public string? Username { get; set; }
        public string? Password { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            return ToDictionary(this);
        }
    }
}
