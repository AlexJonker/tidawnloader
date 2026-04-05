using Microsoft.EntityFrameworkCore;
using Tidawnloader.Models;

namespace Tidawnloader.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<DbAlbum> Albums { get; set; }
    public DbSet<DbArtist> Artists { get; set; }
    public DbSet<DbTrack> Tracks { get; set; }
}
