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

    [JsonPropertyName("trackNumber")]
    public int TrackNumber { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("audioQuality")]
    public string AudioQuality { get; set; } = "";

    [JsonPropertyName("artist")]
    public Track_Artist Artist { get; set; } = new();

    [JsonPropertyName("album")]
    public Track_Album Album { get; set; } = new();

    [JsonPropertyName("manifest")]
    public string? Manifest { get; set; }

    [JsonPropertyName("manifestMimeType")]
    public string? ManifestMimeType { get; set; }


    // Ezra pookie bear
    [JsonPropertyName("bpm")]
    public int? Bpm { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("keyScale")]
    public string? KeyScale { get; set; }

}

public class Track_Artist
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

public class Track_Album
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("cover")]
    public string Cover { get; set; } = "";
}
