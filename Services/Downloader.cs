using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
    private readonly ILogger<Downloader> _logger;
    private readonly string _downloadPath;

    private static readonly string[] apis =
    [
        "https://hifi-one.spotisaver.net",
        "https://hifi-two.spotisaver.net",
        "https://eu-central.monochrome.tf",
        "https://us-west.monochrome.tf",
        "https://api.monochrome.tf",
        "https://monochrome-api.samidy.com",
        "https://tidal.kinoplus.online"
    ];

    public Downloader(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<Downloader> logger)
    {
        _http = httpClientFactory;
        _logger = logger;

        _downloadPath = config["DownloadPath"] ?? "./downloads";
    }

    public async Task DownloadAsync(
        string input,
        IProgress<DownloadState> progress,
        CancellationToken ct = default)
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

        var trackIdTidal = match.Groups[1].Value;

        progress.Report(new DownloadState
        {
            Status = DownloadStatus.GettingStream,
            Message = $"Getting stream (id: {trackIdTidal})..."
        });

        var (streamUrl, mirror) = await FindStream(client, trackIdTidal, ct);

        if (streamUrl is null)
        {
            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = "No mirror returned a stream URL"
            });
            return;
        }

        await DownloadTrack(client, streamUrl, trackIdTidal, mirror!, progress, ct);
    }

    private async Task<(string? url, string? mirror)> FindStream(HttpClient client, string id, CancellationToken ct)
    {
        foreach (var mirror in apis)
        {
            foreach (var quality in new[] { "HI_RES", "LOSSLESS" })
            {
                try
                {
                    var endpoint = $"{mirror}/track/?id={id}&quality={quality}";
                    var resp = await client.GetAsync(endpoint, ct);

                    if (!resp.IsSuccessStatusCode)
                        continue;

                    var body = await resp.Content.ReadAsStringAsync(ct);

                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    var url = ParseStream(root);
                    if (url != null)
                        return (url, mirror);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Mirror {Mirror} failed for track {TrackId} ({Quality})",
                        mirror, id, quality);
                }
            }
        }

        return (null, null);
    }

    private static string? ParseStream(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            var data = root.TryGetProperty("data", out var d) ? d : root;

            if (!data.TryGetProperty("manifest", out var manifest) ||
                !data.TryGetProperty("manifestMimeType", out var mime))
                return null;

            var mimeType = mime.GetString() ?? "";

            var manifestJson = Encoding.UTF8.GetString(
                Convert.FromBase64String(manifest.GetString() ?? "")
            );

            if (mimeType.Contains("bts", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.Parse(manifestJson);

                if (doc.RootElement.TryGetProperty("urls", out var urls) &&
                    urls.GetArrayLength() > 0)
                {
                    return urls[0].GetString();
                }
            }

            if (mimeType.Contains("dash", StringComparison.OrdinalIgnoreCase))
            {
                return ParseDash(manifestJson);
            }
        }

        return null;
    }

    private async Task DownloadTrack(
        HttpClient client,
        string streamUrl,
        string id,
        string mirror,
        IProgress<DownloadState> progress,
        CancellationToken ct)
    {
        progress.Report(new DownloadState
        {
            Status = DownloadStatus.Downloading,
            Message = $"Downloading from {mirror}..."
        });

        Directory.CreateDirectory(_downloadPath);

        // TODO: proper folder structure and file names
        var filePath = Path.Combine(_downloadPath, $"{id}.flac");

        try
        {
            using var response = await client.GetAsync(
                streamUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );

            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? 0;

            await using var input = await response.Content.ReadAsStreamAsync(ct);
            await using var output = File.Create(filePath);

            var buffer = new byte[81920];
            long downloaded = 0;

            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), ct);

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
                "Download failed for track {TrackId} from mirror {Mirror}",
                id, mirror);

            progress.Report(new DownloadState
            {
                Status = DownloadStatus.Failed,
                Error = ex.Message
            });
        }
    }

    private static string? ParseDash(string mpdXml)
    {
        var doc = XDocument.Parse(mpdXml);

        var baseUrl = doc.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "BaseURL")
            ?.Value;

        return baseUrl;
    }
}