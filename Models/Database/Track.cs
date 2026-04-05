using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tidawnloader.Models;

public class DbTrack
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // No auto increment since we use the tidal id here.
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int Duration { get; set; }

    public int TrackNumber { get; set; }

    public string Url { get; set; } = "";

    public string AudioQuality { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int AlbumId { get; set; }

    [ForeignKey(nameof(AlbumId))]
    public DbAlbum? Album { get; set; }

    public int ArtistId { get; set; }

    [ForeignKey(nameof(ArtistId))]
    public DbArtist? Artist { get; set; }
}
