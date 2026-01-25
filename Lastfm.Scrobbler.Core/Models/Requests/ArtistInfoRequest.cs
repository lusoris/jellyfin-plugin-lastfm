using System.Text.Json.Serialization;

namespace Lastfm.Scrobbler.Core.Models.Requests;

public class ArtistInfoRequest : BaseRequest
{
    public override string Method => "artist.getInfo";

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    public override Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            { "method", Method },
            { "artist", Artist }
        };

        if (!string.IsNullOrEmpty(Username))
        {
            dict.Add("username", Username ?? string.Empty);
        }

        return dict;
    }
}
