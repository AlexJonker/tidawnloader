using System.Text;
using System.Text.Json;
using System.Xml;
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

        var track = await _request.Make<Track>($"info?id={Uri.EscapeDataString(trackId)}");

        if (track is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "Track not found"
            });
            return;
        }

        var streamData = await ResolveStream(track, trackId, progress);

        if (streamData is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "No API response"
            });
            return;
        }

        var source = ParseManifest(streamData);

        if (source is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "No mirror returned a stream URL"
            });
            return;
        }

        await DownloadTrack(client, source, trackId, track, progress);
    }

    private async Task<Track?> ResolveStream(Track track, string trackId, IProgress<DownloadState> progress)
    {

        for (int attempt = 0; attempt < 4; attempt++) // Try downloading 4 times
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.GettingStream,
                Message = $"Getting stream (quality: {track.AudioQuality}, attempt {attempt + 1})..."
            });

            var streamData = await _request.Make<Track>(
                $"track?id={Uri.EscapeDataString(trackId)}&quality={Uri.EscapeDataString(track.AudioQuality)}");

            if (streamData is null) continue;

            if (!IsPreview(streamData, track))
                return streamData;

            _logger.LogInformation(
                "Track {TrackId} only returned a preview at quality {Quality}, retrying",
                trackId, track.AudioQuality);
        }

        return null;
    }

    private static bool IsPreview(Track streamData, Track track)
    {
        if (string.Equals(streamData.AssetPresentation, "PREVIEW", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(streamData.Manifest) && track.Duration > 0)
        {
            var manifest = DecodeManifest(streamData.Manifest);
            if (manifest is not null && manifest.Contains("<MPD", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var durationAttr = XDocument.Parse(manifest)
                        .Root?.Attribute("mediaPresentationDuration")?.Value;
                    if (!string.IsNullOrEmpty(durationAttr))
                    {
                        var seconds = XmlConvert.ToTimeSpan(durationAttr).TotalSeconds;
                        if (seconds < track.Duration * 0.6)
                            return true;
                    }
                }
                catch { }
            }
        }

        return false;
    }

    private static StreamSource? ParseManifest(Track streamData)
    {
        if (string.IsNullOrEmpty(streamData.Manifest) || string.IsNullOrEmpty(streamData.ManifestMimeType))
            return null;

        var mimeType = streamData.ManifestMimeType;
        var manifest = DecodeManifest(streamData.Manifest);
        if (manifest is null) return null;

        if (mimeType.Contains("dash", StringComparison.OrdinalIgnoreCase) ||
            manifest.Contains("<MPD", StringComparison.OrdinalIgnoreCase))
        {
            return new StreamSource { ManifestXml = manifest };
        }

        if (mimeType.Contains("bts", StringComparison.OrdinalIgnoreCase) ||
            manifest.TrimStart().StartsWith("{"))
        {
            try
            {
                using var doc = JsonDocument.Parse(manifest);
                if (doc.RootElement.TryGetProperty("urls", out var urls))
                {
                    var url = urls.EnumerateArray()
                        .Select(x => x.GetString())
                        .FirstOrDefault(s => !string.IsNullOrEmpty(s));
                    if (!string.IsNullOrEmpty(url))
                        return new StreamSource { DirectUrl = url };
                }
            }
            catch { }
        }

        return null;
    }

    private static string? DecodeManifest(string manifest)
    {
        var buffer = new byte[manifest.Length];
        if (Convert.TryFromBase64String(manifest, buffer, out int bytesWritten))
            return Encoding.UTF8.GetString(buffer, 0, bytesWritten);

        return manifest;
    }

    private async Task DownloadTrack(
        HttpClient client,
        StreamSource source,
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

        var filePath = Path.Combine(downloadPath, $"{track.Title}.flac");
        var tempPath = Path.Combine(_tempFolder, $"{id}_temp.flac");
        var metaTempPath = Path.Combine(_tempFolder, $"{id}_meta.flac");
        var coverPath = Path.Combine(_tempFolder, $"{id}_cover.jpg");


        try
        {
            if (source.DirectUrl is not null)
            {
                await DownloadDirect(client, source.DirectUrl, tempPath, progress);
            }
            else if (source.ManifestXml is not null)
            {
                await DownloadDash(source.ManifestXml, track, tempPath, progress);
            }
            else
            {
                throw new Exception("No usable stream source");
            }

            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Downloading,
                Message = "Adding metadata..."
            });


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

    private async Task DownloadDirect(
        HttpClient client,
        string streamUrl,
        string tempPath,
        IProgress<DownloadState> progress)
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
    }

    // Tidal returns segmented DASH manifests. Let ffmpeg pull and mux the
    // segments so we don't have to implement the DASH client ourselves.
    private async Task DownloadDash(
        string manifestXml,
        Track track,
        string tempPath,
        IProgress<DownloadState> progress)
    {
        var mpdPath = Path.Combine(_tempFolder, $"{track.Id}_stream.mpd");
        if (File.Exists(mpdPath)) File.Delete(mpdPath);
        await File.WriteAllTextAsync(mpdPath, manifestXml);

        var duration = track.Duration > 0
            ? TimeSpan.FromSeconds(track.Duration)
            : TimeSpan.FromSeconds(300);
        var isFlac = IsFlacManifest(manifestXml);

        var processor = FFMpegArguments
            .FromFileInput(mpdPath, true, options => options
                .WithCustomArgument("-protocol_whitelist file,http,https,tcp,tls,crypto"))
            .OutputToFile(tempPath, true, options =>
            {
                if (isFlac)
                    options.WithCustomArgument("-c copy");
                else
                    options.WithAudioCodec("flac");
            });

        var outputBuilder = new StringBuilder();
        processor
            .NotifyOnOutput(line => outputBuilder.AppendLine(line))
            .NotifyOnProgress(pct => progress.Report(new DownloadState
            {
                Status = DownloadStatus.Downloading,
                ProgressPercent = pct,
                Message = $"Downloading... {pct:F0}%"
            }), duration);

        var success = await processor.ProcessAsynchronously(false);

        File.Delete(mpdPath);

        if (!success || !File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
        {
            var output = outputBuilder.ToString();
            var tail = string.IsNullOrEmpty(output)
                ? ""
                : $" ({output[^Math.Min(300, output.Length)..]})";
            throw new Exception($"FFmpeg failed to download the DASH stream{tail}");
        }
    }

    private static bool IsFlacManifest(string manifestXml)
    {
        try
        {
            return XDocument.Parse(manifestXml).Descendants()
                .Any(x => x.Name.LocalName == "Representation" &&
                          (x.Attribute("codecs")?.Value ?? "")
                          .Contains("flac", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class StreamSource
{
    public string? DirectUrl { get; init; }
    public string? ManifestXml { get; init; }
}