using System.Text.Json.Serialization;

namespace Tidawnloader.Models;

public class ArtistSearchResponse
{
    [JsonPropertyName("artists")]
    public ArtistSearchResults Artists { get; set; } = new();
}

public class ArtistSearchResults
{
    [JsonPropertyName("items")]
    public List<ArtistSearchItem> Items { get; set; } = new();
}

public class ArtistSearchItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}
