using System.Text.Json;
using Tidawnloader.Models;

namespace Tidawnloader.Services;

public class Request
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<Request> _logger;
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

    public Request(
        IHttpClientFactory httpClientFactory,
        ILogger<Request> logger)
    {
        _http = httpClientFactory;
        _logger = logger;
    }

    public async Task<Track?> Make(string endpoint)
    {
        foreach (var api in apis)
        {
            try
            {
                var url = $"{api}/{endpoint}";
                _logger.LogError($"Requesting endpoint: {url}");
                var resp = await _http.CreateClient("Default").GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError($"{api} failed with {resp.StatusCode} and {resp.Content}");
                    continue;
                }

                var body = await resp.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);

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
