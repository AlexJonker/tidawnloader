using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Tidawnloader.Models;

using FFMpegCore;

namespace Tidawnloader.Services;

public enum DownloadStatus
{
    Idle,
    Resolving,
    GettingStream,
    Downloading,
    Done,
    Failed
}

public class DownloadState
{
    public DownloadStatus Status { get; set; } = DownloadStatus.Idle;
    public string Message { get; set; } = "";
    public string? FilePath { get; set; }
    public string? Error { get; set; }
    public double ProgressPercent { get; set; }
}

public class Downloader
{
    private readonly IHttpClientFactory _http;
    private readonly Request _request;
    private readonly ILogger<Downloader> _logger;
    private readonly string _downloadFolder;
    private readonly string _tempFolder;
    public Downloader(
        IHttpClientFactory httpClientFactory,
        Request request,
        IConfiguration config,
        ILogger<Downloader> logger)
    {
        _http = httpClientFactory;
        _request = request;
        _logger = logger;

        _downloadFolder = config["DownloadPath"]!;
        _tempFolder = config["TempPath"]!;
    }

    public async Task DownloadAsync(string input, IProgress<DownloadState> progress)
    {
        var client = _http.CreateClient("Default");

        progress.Report(new DownloadState
        {
            Status = DownloadStatus.Resolving,
            Message = "Resolving track..."
        });

        var trackId = input.Trim();
        if (trackId is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "Track not available"
            });
            return;
        }

        progress.Report(new DownloadState
        {
            Status = DownloadStatus.GettingStream,
            Message = $"Getting stream (id: {trackId})..."
        });

        var track = await _request.Make($"info?id={Uri.EscapeDataString(trackId)}");

        if (track is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "Track not found"
            });
            return;
        }

        var streamData = await _request.Make($"track?id={Uri.EscapeDataString(trackId)}&quality={Uri.EscapeDataString(track.AudioQuality ?? "")}");

        if (streamData is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "No API response"
            });
            return;
        }

        string? baseUrl = null;

        if (!string.IsNullOrEmpty(streamData.Manifest) && !string.IsNullOrEmpty(streamData.ManifestMimeType))
        {
            var mimeType = streamData.ManifestMimeType;

            var manifestJson = Encoding.UTF8.GetString(
                Convert.FromBase64String(streamData.Manifest)
            );

            if (mimeType.Contains("bts", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.Parse(manifestJson);

                if (doc.RootElement.TryGetProperty("urls", out var urls) &&
                    urls.GetArrayLength() > 0)
                    baseUrl = urls[0].GetString();
            }
            else if (mimeType.Contains("dash", StringComparison.OrdinalIgnoreCase))
            {
                var doc = XDocument.Parse(manifestJson);
                var segmentTemplate = doc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "SegmentTemplate");
                
                if (segmentTemplate != null)
                {
                    var mediaAttr = segmentTemplate.Attribute("media")?.Value;
                    if (!string.IsNullOrEmpty(mediaAttr))
                    {
                        baseUrl = mediaAttr;
                    }
                }
                
                if (baseUrl is null)
                {
                    baseUrl = doc.Descendants()
                        .FirstOrDefault(x => x.Name.LocalName == "BaseURL")
                        ?.Value;
                }
            }
        }

        if (baseUrl is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "No mirror returned a stream URL"
            });
            return;
        }

        await DownloadTrack(client, baseUrl, trackId, track, progress);
    }

    private async Task DownloadTrack(
        HttpClient client,
        string streamUrl,
        string id,
        Track track,
        IProgress<DownloadState> progress)
    {
        progress.Report(new DownloadState
        {
            Status = DownloadStatus.Downloading,
            Message = $"Downloading..."
        });

        var downloadPath = Path.Combine(_downloadFolder, $"{track.Artist.Name}", $"{track.Album.Title}");

        Directory.CreateDirectory(downloadPath);
        Directory.CreateDirectory(_tempFolder);

        // TODO: proper folder structure and file names
        var filePath = Path.Combine(downloadPath, $"{track.Title}.flac");
        var tempPath = Path.Combine(_tempFolder, $"{id}_temp.flac");
        var metaTempPath = Path.Combine(_tempFolder, $"{id}_meta.flac");
        var coverPath = Path.Combine(_tempFolder, $"{id}_cover.jpg");


        try
        {
            using var response = await client.GetAsync(
                streamUrl,
                HttpCompletionOption.ResponseHeadersRead
            );

            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? 0;

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(tempPath);

            var buffer = new byte[81920];
            long downloaded = 0;

            int read;
            while ((read = await input.ReadAsync(buffer)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read));

                downloaded += read;

                if (total > 0)
                {
                    progress.Report(new DownloadState
                    {
                        Status = DownloadStatus.Downloading,
                        ProgressPercent = downloaded * 100.0 / total,
                        Message =
                            $"{downloaded / 1_048_576.0:F1} MB / {total / 1_048_576.0:F1} MB"
                    });
                }
            }

            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Downloading,
                Message = "Adding metadata..."
            });


            // TODO: maybe more here like date, year, genre, lyrics
            // Is a bit more work since it has to come from the album api instead of the track api.
            // More album data: https://hifi-one.spotisaver.net/album/?id=1225569
            // Lycrics: https://lyrics-api.binimum.org/?track=Too Sweet&artist=Isaa Corva&album=Too Sweet&duration=253
            var metadataArgs = "";

            metadataArgs += $"-metadata tidal_id=\"{id}\" ";
            metadataArgs += $"-metadata title=\"{track.Title}\" ";
            metadataArgs += $"-metadata artist=\"{track.Artist.Name}\" ";
            metadataArgs += $"-metadata albumartist=\"{track.Artist.Name}\" ";
            metadataArgs += $"-metadata album=\"{track.Album.Title}\" ";
            metadataArgs += $"-metadata tracknumber=\"{track.TrackNumber}\" ";
            metadataArgs += $"-metadata comment=\"https://github.com/alexjonker/tidawnloader\" ";

            var coverUrl = $"https://resources.tidal.com/images/{track.Album.Cover.Replace("-", "/")}/1280x1280.jpg";
            if (!string.IsNullOrEmpty(track.Album.Cover))
            {
                try
                {
                    var coverBytes = await client.GetByteArrayAsync(coverUrl);
                    await File.WriteAllBytesAsync(coverPath, coverBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download cover image");
                }
            }

            if (File.Exists(coverPath))
            {
                await FFMpegArguments
                    .FromFileInput(tempPath)
                    .OutputToFile(metaTempPath, false, options => options
                        .WithCustomArgument($"-i \"{coverPath}\" {metadataArgs}-c copy -map 0 -map_metadata 0 -map 1 -disposition:v attached_pic"))
                    .ProcessAsynchronously();

                File.Delete(coverPath);
            }
            else
            {
                await FFMpegArguments
                    .FromFileInput(tempPath)
                    .OutputToFile(metaTempPath, false, options => options
                        .WithCustomArgument(metadataArgs + "-c copy"))
                    .ProcessAsynchronously();
            }

            if (!File.Exists(metaTempPath))
            {
                throw new Exception("FFmpeg failed to produce output file");
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(metaTempPath, filePath);
            File.Delete(tempPath);

            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Done,
                FilePath = filePath,
                Message = "Download finished",
                ProgressPercent = 100
            });
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (File.Exists(filePath)) File.Delete(filePath);
            if (File.Exists(coverPath)) File.Delete(coverPath);
            if (File.Exists(metaTempPath)) File.Delete(metaTempPath);

            _logger.LogError(ex,
                "Download failed for track {TrackId}",
                id);

            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = ex.Message
            });
        }
    }
}