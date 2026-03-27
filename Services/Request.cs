using System.Text.Json;
using Tidawnloader.Models;

namespace Tidawnloader.Services;

public class Request
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<Request> _logger;

    public Request(
        IHttpClientFactory httpClientFactory,
        ILogger<Request> logger)
    {
        _http = httpClientFactory;
        _logger = logger;
    }

    private static readonly string[] UptimeApiUrls =
    [
        "https://tidal-uptime.jiffy-puffs-1j.workers.dev/",
        "https://tidal-uptime.props-76styles.workers.dev/",
    ];

    private static readonly string[] FallbackApis =
    [
        "https://api.monochrome.tf",
        "https://hifi-one.spotisaver.net",
        "https://hifi-two.spotisaver.net",
        "https://eu-central.monochrome.tf",
        "https://us-west.monochrome.tf",
        "https://monochrome-api.samidy.com",
        "https://tidal.kinoplus.online",
    ];

    private static List<string>? _apis;

    public async Task<Track?> Make(string endpoint)
    {
        if (_apis == null)
        {
            foreach (var url in UptimeApiUrls.OrderBy(_ => Random.Shared.Next()))
            {
                try
                {
                    var resp = await _http.CreateClient("Default").GetAsync(url);
                    _logger.LogDebug($"Fetching uptime API from {url}");

                    if (!resp.IsSuccessStatusCode) continue;

                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    if (!doc.RootElement.TryGetProperty("api", out var apiArray)) continue;

                    var instances = apiArray.EnumerateArray()
                        .Select(item => item.TryGetProperty("url", out var u) ? u.GetString()
                                      : item.ValueKind == JsonValueKind.String ? item.GetString()
                                      : null)
                        .Where(s => s != null)
                        .Select(s => s!)
                        .ToList();

                    if (instances.Count == 0) continue;
                    _apis = instances;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch from {Url}", url);
                }
            }

            if (_apis == null)
            {
                _logger.LogWarning("Failed to load instances from all uptime APIs, using fallbacks");
                _apis = FallbackApis.ToList();
            }
        }

        foreach (var api in _apis)
        {
            try
            {
                var url = $"{api}/{endpoint}";
                _logger.LogDebug($"Requesting endpoint: {url}");
                var resp = await _http.CreateClient("Default").GetAsync(url);
                _logger.LogDebug($"Fetching {url}");

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError($"{api} failed with {resp.StatusCode} and {resp.Content}");
                    continue;
                }

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());

                if (doc.RootElement.TryGetProperty("detail", out _))
                    continue;

                var data = doc.RootElement.GetProperty("data");
                return JsonSerializer.Deserialize<Track>(data.GetRawText());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mirror {Mirror} failed", api);
            }
        }

        return null;
    }
}
