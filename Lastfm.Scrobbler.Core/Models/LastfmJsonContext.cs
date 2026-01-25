// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Lastfm.Scrobbler.Core.Models;

using System.Text.Json.Serialization;
using Lastfm.Scrobbler.Core.Models.Responses;

/// <summary>
/// Source-generated JSON serializer context for all Last.fm API types.
/// Eliminates runtime reflection, reduces startup time, and enables AOT compilation.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BaseResponse))]
[JsonSerializable(typeof(MobileSessionResponse))]
[JsonSerializable(typeof(ScrobbleResponse))]
[JsonSerializable(typeof(LovedTracksResponse))]
[JsonSerializable(typeof(TopTracksResponse))]
[JsonSerializable(typeof(SimilarArtistsResponse))]
[JsonSerializable(typeof(SimilarTracksResponse))]
[JsonSerializable(typeof(ArtistInfoResponse))]
[JsonSerializable(typeof(AlbumInfoResponse))]
[JsonSerializable(typeof(WeeklyTrackChartResponse))]
[JsonSerializable(typeof(UserTopTagsResponse))]
[JsonSerializable(typeof(TagTopTracksResponse))]
[JsonSerializable(typeof(List<Scrobble>))]
[JsonSerializable(typeof(LastfmImage), TypeInfoPropertyName = "LastfmImageModel")]
[JsonSerializable(typeof(List<LastfmImage>), TypeInfoPropertyName = "ListLastfmImageModel")]
[JsonSerializable(typeof(PaginationAttributes))]
[JsonSerializable(typeof(TagsInfo), TypeInfoPropertyName = "TagsInfoModel")]
public partial class LastfmJsonContext : JsonSerializerContext
{
}
