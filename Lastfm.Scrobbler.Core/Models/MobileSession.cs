using System.Text.Json.Serialization;

namespace Lastfm.Scrobbler.Core.Models
{
    public class MobileSession
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("subscriber")]
        public int Subscriber { get; set; }
    }
}
