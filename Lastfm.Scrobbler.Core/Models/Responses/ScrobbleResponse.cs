// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Text.Json.Serialization;

namespace Lastfm.Scrobbler.Core.Models.Responses
{
    public class ScrobbleResponse : BaseResponse
    {
        [JsonPropertyName("scrobbles")]
        public ScrobblesData? Scrobbles { get; set; }
    }

    public class ScrobblesData
    {
        [JsonPropertyName("@attr")]
        public ScrobbleAttributes? Attributes { get; set; }

        [JsonPropertyName("scrobble")]
        public ScrobbleResponseItem? Scrobble { get; set; }
    }

    public class ScrobbleResponseItem
    {
        [JsonPropertyName("artist")]
        public ScrobbleResponseItemDetails? Artist { get; set; }

        [JsonPropertyName("album")]
        public ScrobbleResponseItemDetails? Album { get; set; }

        [JsonPropertyName("track")]
        public ScrobbleResponseItemDetails? Track { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    public class ScrobbleResponseItemDetails
    {
        [JsonPropertyName("#text")]
        public string? Text { get; set; }
    }

    public class ScrobbleAttributes
    {
        [JsonPropertyName("accepted")]
        public int Accepted { get; set; }

        [JsonPropertyName("ignored")]
        public int Ignored { get; set; }
    }
}
