using System.Text.Json;

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

    public async Task<JsonElement?> Make(string endpoint) // Make a request
    {
        foreach (var api in apis)
        {
            try
            {
                var url = $"{api}/{endpoint}";
                var resp = await _http.CreateClient("Default").GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                    continue;

                var body = await resp.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);

                var root = doc.RootElement;

                return root.Clone();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mirror {Mirror} failed", api);
            }

        }
        return null;
    }
}