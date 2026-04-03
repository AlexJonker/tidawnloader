using System.Text.Json.Serialization;

namespace Tidawnloader.Models;

public class Album
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("numberOfTracks")]
    public int NumberOfTracks { get; set; }

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("cover")]
    public string Cover { get; set; } = "";

    [JsonPropertyName("artist")]
    public Album_Artist Artist { get; set; } = new();

    [JsonPropertyName("artists")]
    public List<Album_Artist> Artists { get; set; } = new();

    [JsonPropertyName("items")]
    public List<Album_Item> Items { get; set; } = new();

    [JsonIgnore]
    public List<Track> AllTracks =>
        Items
            .Select(item => item.Track)
            .Where(track => track is not null)
            .ToList();
}

public class Album_Artist
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}
public class Album_Item
{
    [JsonPropertyName("item")]
    public Track Track { get; set; } = new();

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}