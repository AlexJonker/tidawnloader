using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tidawnloader.Models;

public class DbAlbum
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // No auto increment since we use the tidal id here.
    public int Id { get; set; }

    public required string Title { get; set; }

    public int Duration { get; set; }

    public int NumberOfTracks { get; set; }

    public required string ReleaseDate { get; set; }

    public required string Cover { get; set; }

    public required string Type { get; set; }

    public int ArtistId { get; set; }

    [ForeignKey(nameof(ArtistId))]
    public DbArtist? Artist { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DbTrack> Tracks { get; set; } = new();
}