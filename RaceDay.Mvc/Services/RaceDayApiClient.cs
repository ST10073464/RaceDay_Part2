using System.Text;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public class RaceDayApiClient
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public RaceDayApiClient(
        HttpClient client,
        IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string endpoint,
        object? body = null)
    {
        using var request =
            new HttpRequestMessage(method, endpoint);

        var session =
            _httpContextAccessor.HttpContext?.Session;

        var cookie =
            session?.GetString("ApiCookie");

        if (!string.IsNullOrWhiteSpace(cookie))
        {
            request.Headers.Add("Cookie", cookie);
        }

        if (body != null)
        {
            string json =
                JsonSerializer.Serialize(body);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");
        }

        return await _client.SendAsync(request);
    }

    public async Task<T?> ReadAsync<T>(
        HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        string json =
            await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<T>(
            json,
            _jsonOptions);
    }
}