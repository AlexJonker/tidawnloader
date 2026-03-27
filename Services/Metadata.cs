using Tidawnloader.Models;

namespace Tidawnloader.Services;

public class TrackInfo
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? ArtistName { get; set; }
    public string? ArtistId { get; set; }
    public string? TrackNumber { get; set; }
    public string? AlbumName { get; set; }
    public string? AlbumId { get; set; }
    public string? CoverUrl { get; set; }
    public int? Duration { get; set; }
    public string? AudioQuality { get; set; }
    public string? Error { get; set; }
}

public class Metadata
{
    private readonly Request _request;

    public Metadata(Request request)
    {
        _request = request;
    }

    public async Task<TrackInfo> GetTrackInfo(string trackId)
    {
        if (string.IsNullOrEmpty(trackId))
        {
            return new TrackInfo { Error = "Invalid track ID" };
        }

        var track = await _request.Make($"info?id={Uri.EscapeDataString(trackId)}");

        if (track is null)
        {
            return new TrackInfo { Error = "No API response" };
        }

        var info = new TrackInfo
        {
            Id = trackId,
            Title = track.Title,
            ArtistName = track.Artist.Name,
            ArtistId = track.Artist.Id.ToString(),
            TrackNumber = track.TrackNumber.ToString(),
            AlbumName = track.Album.Title,
            AlbumId = track.Album.Id.ToString(),
            CoverUrl = $"https://resources.tidal.com/images/{track.Album.Cover.Replace("-", "/")}/1280x1280.jpg",
            Duration = track.Duration,
            AudioQuality = track.AudioQuality
        };

        return info;
    }
}