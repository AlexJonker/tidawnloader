using System.ComponentModel.DataAnnotations;

namespace Tidawnloader.Models;

public class DbArtist
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public string? Picture { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DbTrack> Tracks { get; set; } = new();
}
