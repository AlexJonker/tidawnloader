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

        var root = await _request.Make($"info?id={trackId}");

        if (root is null)
        {
            return new TrackInfo { Error = "No API response" };
        }

        if (!root.Value.TryGetProperty("data", out var data))
        {
            var error = root.Value.TryGetProperty("detail", out var detail)
                ? detail.GetString()
                : "Invalid API response";

            return new TrackInfo { Error = error };
        }

        var info = new TrackInfo { Id = trackId };

        if (data.TryGetProperty("title", out var title))
            info.Title = title.GetString();

        if (data.TryGetProperty("artist", out var artistObj))
        {
            if (artistObj.TryGetProperty("name", out var artistName))
                info.ArtistName = artistName.GetString();

            if (artistObj.TryGetProperty("id", out var artistId))
                info.ArtistId = artistId.GetString();
        }

        if (data.TryGetProperty("trackNumber", out var trackNumber))
            info.TrackNumber = trackNumber.GetInt32().ToString();

        if (data.TryGetProperty("album", out var albumObj))
        {
            if (albumObj.TryGetProperty("title", out var albumTitle))
                info.AlbumName = albumTitle.GetString();

            if (albumObj.TryGetProperty("id", out var albumId))
                info.AlbumId = albumId.GetString();

            if (albumObj.TryGetProperty("cover", out var coverId))
            {
                var cover = coverId.GetString();
                if (!string.IsNullOrEmpty(cover))
                    info.CoverUrl = $"https://resources.tidal.com/images/{cover.Replace("-", "/")}/1280x1280.jpg";
            }
        }

        if (data.TryGetProperty("duration", out var duration))
            info.Duration = duration.GetInt32();

        if (data.TryGetProperty("audioQuality", out var audioQuality))
            info.AudioQuality = audioQuality.GetString();

        return info;
    }
}