using System.ComponentModel.DataAnnotations;

namespace Tidawnloader.Models;

public class DbAlbum
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int Duration { get; set; }

    public int NumberOfTracks { get; set; }

    public string ReleaseDate { get; set; } = "";

    public string Url { get; set; } = "";

    public string Cover { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DbTrack> Tracks { get; set; } = new();
}