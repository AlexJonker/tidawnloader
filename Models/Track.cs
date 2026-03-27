using System.Text.Json.Serialization;

namespace Tidawnloader.Models;

public class Track
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("replayGain")]
    public double ReplayGain { get; set; }

    [JsonPropertyName("peak")]
    public double Peak { get; set; }

    [JsonPropertyName("allowStreaming")]
    public bool AllowStreaming { get; set; }

    [JsonPropertyName("streamReady")]
    public bool StreamReady { get; set; }

    [JsonPropertyName("payToStream")]
    public bool PayToStream { get; set; }

    [JsonPropertyName("adSupportedStreamReady")]
    public bool AdSupportedStreamReady { get; set; }

    [JsonPropertyName("djReady")]
    public bool DjReady { get; set; }

    [JsonPropertyName("stemReady")]
    public bool StemReady { get; set; }

    [JsonPropertyName("streamStartDate")]
    public string StreamStartDate { get; set; } = "";

    [JsonPropertyName("premiumStreamingOnly")]
    public bool PremiumStreamingOnly { get; set; }

    [JsonPropertyName("trackNumber")]
    public int TrackNumber { get; set; }

    [JsonPropertyName("volumeNumber")]
    public int VolumeNumber { get; set; }

    [JsonPropertyName("version")]
    public object? Version { get; set; }

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

    [JsonPropertyName("copyright")]
    public string Copyright { get; set; } = "";

    [JsonPropertyName("bpm")]
    public int? Bpm { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("keyScale")]
    public string? KeyScale { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("isrc")]
    public string? Isrc { get; set; }

    [JsonPropertyName("editable")]
    public bool Editable { get; set; }

    [JsonPropertyName("explicit")]
    public bool Explicit { get; set; }

    [JsonPropertyName("audioQuality")]
    public string AudioQuality { get; set; } = "";

    [JsonPropertyName("audioModes")]
    public List<string> AudioModes { get; set; } = [];

    [JsonPropertyName("mediaMetadata")]
    public MediaMetadata? MediaMetadata { get; set; }

    [JsonPropertyName("upload")]
    public bool Upload { get; set; }

    [JsonPropertyName("accessType")]
    public string AccessType { get; set; } = "";

    [JsonPropertyName("spotlighted")]
    public bool Spotlighted { get; set; }

    [JsonPropertyName("artist")]
    public ArtistInfo Artist { get; set; } = new();

    [JsonPropertyName("artists")]
    public List<ArtistInfo> Artists { get; set; } = [];

    [JsonPropertyName("album")]
    public AlbumInfo Album { get; set; } = new();

    [JsonPropertyName("mixes")]
    public Dictionary<string, string> Mixes { get; set; } = new();

    [JsonPropertyName("manifest")]
    public string? Manifest { get; set; }

    [JsonPropertyName("manifestMimeType")]
    public string? ManifestMimeType { get; set; }
}

public class MediaMetadata
{
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

public class ArtistInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("handle")]
    public object? Handle { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

public class AlbumInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("cover")]
    public string Cover { get; set; } = "";

    [JsonPropertyName("vibrantColor")]
    public string? VibrantColor { get; set; }

    [JsonPropertyName("videoCover")]
    public object? VideoCover { get; set; }
}
