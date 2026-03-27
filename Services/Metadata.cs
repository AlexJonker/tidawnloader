using System.Text.Json;

namespace Tidawnloader.Services;

public class TrackInfo
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? CoverUrl { get; set; }
    public int? Duration { get; set; }
    public string? Error { get; set; }
}

public class Metadata
{
    private readonly Request _request;

    public Metadata(Request request)
    {
        _request = request;
    }

    public async Task<TrackInfo> GetInfo(string trackId)
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

        if (data.TryGetProperty("artist", out var artistObj) && artistObj.TryGetProperty("name", out var artistName))
            info.Artist = artistName.GetString();

        if (data.TryGetProperty("album", out var albumObj))
        {
            if (albumObj.TryGetProperty("title", out var albumTitle))
                info.Album = albumTitle.GetString();

            if (albumObj.TryGetProperty("cover", out var coverId))
            {
                var cover = coverId.GetString();
                if (!string.IsNullOrEmpty(cover))
                    info.CoverUrl = $"https://resources.tidal.com/images/{cover.Replace("-", "/")}/320x320.jpg";
            }
        }

        if (data.TryGetProperty("duration", out var duration))
            info.Duration = duration.GetInt32();

        return info;
    }
}