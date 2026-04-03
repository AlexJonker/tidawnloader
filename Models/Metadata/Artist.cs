using System.Text.Json.Serialization;

namespace Tidawnloader.Models;

public class Artist
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    [JsonPropertyName("artistRoles")]
    public List<ArtistRole> ArtistRoles { get; set; } = new();
}

public class ArtistRole
{
    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
}
