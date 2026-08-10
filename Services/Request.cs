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

    private static readonly string[] HiFiUrls =
    [
        "https://lol.samidy.workers.dev",
    ];

    public async Task<T?> Make<T>(string endpoint)
    {
        foreach (var api in HiFiUrls)
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

                try
                {
                    // TODO: remove the data and artist ones and use RootElement for all.
                    if (doc.RootElement.TryGetProperty("data", out var data))
                        return JsonSerializer.Deserialize<T>(data.GetRawText());

                    if (doc.RootElement.TryGetProperty("artist", out var artist))
                        return JsonSerializer.Deserialize<T>(artist.GetRawText());

                    return JsonSerializer.Deserialize<T>(doc.RootElement.GetRawText());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load data from {Mirror}", api);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mirror {Mirror} failed", api);
            }
        }

        return default;
    }
}
