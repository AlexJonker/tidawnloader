using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Tidawnloader.Services;

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
    private readonly string _downloadPath;

    public Downloader(
        IHttpClientFactory httpClientFactory,
        Request request,
        IConfiguration config,
        ILogger<Downloader> logger)
    {
        _http = httpClientFactory;
        _request = request;
        _logger = logger;

        _downloadPath = config["DownloadPath"] ?? "./downloads";
    }

    public async Task DownloadAsync(string input, IProgress<DownloadState> progress)
    {
        var client = _http.CreateClient("Default");

        progress.Report(new DownloadState
        {
            Status = DownloadStatus.Resolving,
            Message = "Resolving track..."
        });

        var trackUrl = input.Trim();
        if (trackUrl is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "Track not available"
            });
            return;
        }

        var match = Regex.Match(trackUrl, @"/track/(\d+)");
        if (!match.Success)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "Couldn't extract track id"
            });
            return;
        }

        var trackId = match.Groups[1].Value;

        progress.Report(new DownloadState
        {
            Status = DownloadStatus.GettingStream,
            Message = $"Getting stream (id: {trackId})..."
        });

        var root = await _request.Make($"track?id={trackId}&quality=LOSSLESS");

        if (root is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "No API response"
            });
            return;
        }

        if (!root.Value.TryGetProperty("data", out var data))
        {
            if (root.Value.TryGetProperty("detail", out var detail))
            {
                progress.Report(new DownloadState
                {
                    Status = DownloadStatus.Failed,
                    Error = detail.GetString() ?? "API error"
                });
                return;
            }

            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "Invalid API response"
            });
            return;
        }

        string? baseUrl = null;

        if (data.TryGetProperty("manifest", out var manifest) &&
            data.TryGetProperty("manifestMimeType", out var mime))
        {
            var mimeType = mime.GetString() ?? "";

            var manifestJson = Encoding.UTF8.GetString(
                Convert.FromBase64String(manifest.GetString() ?? "")
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

        await DownloadTrack(client, baseUrl, trackId, progress);
    }

    private async Task DownloadTrack(
        HttpClient client,
        string streamUrl,
        string id,
        IProgress<DownloadState> progress)
    {
        progress.Report(new DownloadState
        {
            Status = DownloadStatus.Downloading,
            Message = $"Downloading..."
        });

        Directory.CreateDirectory(_downloadPath);

        // TODO: proper folder structure and file names
        var filePath = Path.Combine(_downloadPath, $"{id}.flac");

        try
        {
            using var response = await client.GetAsync(
                streamUrl,
                HttpCompletionOption.ResponseHeadersRead
            );

            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? 0;

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(filePath);

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
                Status = DownloadStatus.Done,
                FilePath = filePath,
                Message = "Download finished",
                ProgressPercent = 100
            });
        }
        catch (Exception ex)
        {
            File.Delete(filePath);

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