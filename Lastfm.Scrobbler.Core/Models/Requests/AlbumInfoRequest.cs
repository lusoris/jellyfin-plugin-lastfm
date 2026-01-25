using System.Text.Json.Serialization;

namespace Lastfm.Scrobbler.Core.Models.Requests;

public class AlbumInfoRequest : BaseRequest
{
    public override string Method => "album.getInfo";

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("album")]
    public string Album { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    public override Dictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            { "method", Method },
            { "artist", Artist },
            { "album", Album }
        };

        if (!string.IsNullOrEmpty(Username))
        {
            dict.Add("username", Username ?? string.Empty);
        }

        return dict;
    }
}
