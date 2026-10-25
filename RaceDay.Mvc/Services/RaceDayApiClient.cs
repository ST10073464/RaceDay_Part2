using System.Text;
using System.Text.Json;
using RaceDay.Mvc.Models;

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

    // ---------------------------------------------------------
    // SEND REQUEST TO RACEDAY API
    // ---------------------------------------------------------

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
                JsonSerializer.Serialize(
                    body,
                    _jsonOptions);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");
        }

        return await _client.SendAsync(request);
    }

    // ---------------------------------------------------------
    // READ JSON RESPONSE
    // ---------------------------------------------------------

    public async Task<T?> ReadAsync<T>(
        HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        string json =
            await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(
            json,
            _jsonOptions);
    }

    // ---------------------------------------------------------
    // GET ALL EVENTS
    // ---------------------------------------------------------

    public async Task<List<EventViewModel>> GetEventsAsync()
    {
        using HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Get,
                "api/events");

        if (!response.IsSuccessStatusCode)
        {
            return new List<EventViewModel>();
        }

        List<EventViewModel>? events =
            await ReadAsync<List<EventViewModel>>(
                response);

        return events ??
               new List<EventViewModel>();
    }

    // ---------------------------------------------------------
    // GET SINGLE EVENT
    // ---------------------------------------------------------

    public async Task<EventViewModel?> GetEventAsync(
        int eventId)
    {
        using HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Get,
                $"api/events/{eventId}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadAsync<EventViewModel>(
            response);
    }

    public async Task<List<EventViewModel>> GetMyEventsAsync()
    {
        var response = await SendAsync(
            HttpMethod.Get,
            "api/events/mine");

        if (!response.IsSuccessStatusCode)
        {
            return new List<EventViewModel>();
        }

        var events =
            await ReadAsync<List<EventViewModel>>(response);

        return events ?? new List<EventViewModel>();
    }
    public async Task<HttpResponseMessage> CreateEventAsync(
    CreateEventViewModel model)
    {
        return await SendAsync(
            HttpMethod.Post,
            "api/events",
            model);
    }

    // ---------------------------------------------------------
    // GET EVENT CATEGORIES
    // ---------------------------------------------------------

    public async Task<List<CategoryViewModel>> GetEventCategoriesAsync(
        int eventId)
    {
        using HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Get,
                $"api/categories/event/{eventId}");

        if (!response.IsSuccessStatusCode)
        {
            return new List<CategoryViewModel>();
        }

        List<CategoryViewModel>? categories =
            await ReadAsync<List<CategoryViewModel>>(
                response);

        return categories ??
               new List<CategoryViewModel>();
    }

    // ------------------------------------------------------------
    // GET MY ENROLMENTS
    // ------------------------------------------------------------

    public async Task<List<EnrolmentViewModel>> GetMyEnrolmentsAsync()
    {
        var response = await SendAsync(
            HttpMethod.Get,
            "api/enrolments/mine");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to retrieve enrolments. " +
                $"API returned {(int)response.StatusCode} {response.StatusCode}.");
        }

        var enrolments =
            await ReadAsync<List<EnrolmentViewModel>>(response);

        return enrolments ?? new List<EnrolmentViewModel>();
    }

    public async Task<EventViewModel?> GetEventByIdAsync(
    int eventId)
    {
        var response = await SendAsync(
            HttpMethod.Get,
            $"api/events/{eventId}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadAsync<EventViewModel>(
            response);
    }

    public async Task<HttpResponseMessage> UpdateEventAsync(
    int eventId,
    EditEventViewModel model)
    {
        return await SendAsync(
            HttpMethod.Put,
            $"api/events/{eventId}",
            model);
    }

    public async Task<HttpResponseMessage> CreateCategoryAsync(
    ManageCategoriesViewModel model)
    {
        var category = new
        {
            model.EventId,
            model.CategoryName,
            model.MinAge,
            model.MaxAge,
            model.DistanceKm
        };

        return await SendAsync(
            HttpMethod.Post,
            "api/categories",
            category);
    }
    public async Task<HttpResponseMessage> DeleteCategoryAsync(
    int categoryId)
    {
        return await SendAsync(
            HttpMethod.Delete,
            $"api/categories/{categoryId}");
    }

    // ------------------------------------------------------------
    // RECORD RESULT
    // ------------------------------------------------------------
    public async Task<HttpResponseMessage> RecordResultAsync(
        RecordResultViewModel model)
    {
        var result = new
        {
            model.EnrolmentId,
            model.FinishTime,
            model.FinishingPosition
        };

        return await SendAsync(
            HttpMethod.Post,
            "api/results",
            result);
    }


    // ---------------------------------------------------------
    // GET ORGANISER EVENT RESULTS
    // ---------------------------------------------------------

    public async Task<List<ResultViewModel>>
        GetEventResultsAsync(int eventId)
    {
        using HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Get,
                $"api/results/event/{eventId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to retrieve event results. " +
                $"API returned {(int)response.StatusCode} " +
                $"{response.StatusCode}.");
        }

        List<ResultViewModel>? results =
            await ReadAsync<List<ResultViewModel>>(
                response);

        return results ??
               new List<ResultViewModel>();
    }


    // ---------------------------------------------------------
    // GET PARTICIPANT RESULTS
    // ---------------------------------------------------------

    public async Task<List<ResultViewModel>>
        GetMyResultsAsync()
    {
        using HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Get,
                "api/results/mine");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to retrieve participant results. " +
                $"API returned {(int)response.StatusCode} " +
                $"{response.StatusCode}.");
        }

        List<ResultViewModel>? results =
            await ReadAsync<List<ResultViewModel>>(
                response);

        return results ??
               new List<ResultViewModel>();
    }

    // ---------------------------------------------------------
    // GET ORGANISER EVENT ENROLMENTS
    // ---------------------------------------------------------

    public async Task<List<EventEnrolmentViewModel>>
        GetEventEnrolmentsAsync(int eventId)
    {
        using HttpResponseMessage response =
            await SendAsync(
                HttpMethod.Get,
                $"api/enrolments/event/{eventId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to retrieve event enrolments. " +
                $"API returned {(int)response.StatusCode} " +
                $"{response.StatusCode}.");
        }

        List<EventEnrolmentViewModel>? enrolments =
            await ReadAsync<List<EventEnrolmentViewModel>>(
                response);

        return enrolments ??
               new List<EventEnrolmentViewModel>();
    }

}