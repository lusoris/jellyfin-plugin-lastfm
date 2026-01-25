using System;
using System.Collections.Generic;
using System.Linq;

namespace Lastfm.Scrobbler.Core.Models;

public class Track
{
    public string? Name { get; set; }
    public List<string>? Artists { get; set; }
    public string? Album { get; set; }
    public long? RunTimeTicks { get; set; }
    public Dictionary<string, string>? ProviderIds { get; set; }
    public List<string>? AlbumArtists { get; set; }
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the primary artist (for scrobbling).
    /// </summary>
    public string Artist => Artists?.FirstOrDefault() ?? AlbumArtists?.FirstOrDefault() ?? string.Empty;

    /// <summary>
    /// Unix timestamp for scrobbling.
    /// </summary>
    public long Timestamp { get; set; }
}
