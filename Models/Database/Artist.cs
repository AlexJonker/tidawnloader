using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tidawnloader.Models;

public class DbArtist
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // No auto increment since we use the tidal id here.
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Url { get; set; } = "";

    public string? Picture { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DbTrack> Tracks { get; set; } = new();
}
