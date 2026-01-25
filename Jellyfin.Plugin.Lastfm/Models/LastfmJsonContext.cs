// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

namespace Jellyfin.Plugin.Lastfm.Models;

using System.Text.Json.Serialization;
using Responses;
using Services;

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
[JsonSerializable(typeof(List<ScrobbleInfo>))]
[JsonSerializable(typeof(PlaylistResult))]
public partial class LastfmJsonContext : JsonSerializerContext
{
}
