
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts.Auth;
using RaceDay.Mvc.Services;
using System.Text.Json;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiClient _apiClient;

        public AccountController(RaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // -------------------------------------------------
        // REGISTER - GET
        // -------------------------------------------------

        [HttpGet]
        public IActionResult Register(int? eventId = null)
        {
            ViewBag.EventId = eventId > 0 ? eventId : null;

            return View(new RegisterRequest());
        }

        // -------------------------------------------------
        // REGISTER - POST
        // -------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterRequest model,
            int? eventId = null)
        {
            ViewBag.EventId = eventId > 0 ? eventId : null;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _apiClient.SendAsync(
                    HttpMethod.Post,
                    "api/Auth/register",
                    model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Registration successful. You can now log in.";

                    // If registration started from an event,
                    // continue to Login with the same event ID.
                    if (eventId > 0)
                    {
                        return RedirectToAction(
                            "Login",
                            "Account",
                            new { eventId });
                    }

                    // Normal registration returns to landing page.
                    return RedirectToAction("Index", "Home");
                }

                string message = await ReadApiMessageAsync(response);

                ModelState.AddModelError(string.Empty, message);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Unable to connect to RaceDay API: {ex.Message}");

                return View(model);
            }
        }

        // -------------------------------------------------
        // LOGIN - GET
        // -------------------------------------------------

        [HttpGet]
        public IActionResult Login(int? eventId = null)
        {
            ViewBag.EventId = eventId > 0 ? eventId : null;

            return View(new LoginRequest());
        }

        // -------------------------------------------------
        // LOGIN - POST
        // -------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginRequest model,
            int? eventId = null)
        {
            ViewBag.EventId = eventId > 0 ? eventId : null;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _apiClient.SendAsync(
                    HttpMethod.Post,
                    "api/Auth/login",
                    model);

                if (!response.IsSuccessStatusCode)
                {
                    string message = await ReadApiMessageAsync(response);

                    ModelState.AddModelError(string.Empty, message);

                    return View(model);
                }

                // Capture authentication cookie returned by API.
                if (response.Headers.TryGetValues(
                    "Set-Cookie",
                    out var cookieHeaders))
                {
                    string? authenticationCookie =
                        cookieHeaders.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(authenticationCookie))
                    {
                        string cookie =
                            authenticationCookie.Split(';')[0];

                        HttpContext.Session.SetString(
                            "ApiCookie",
                            cookie);
                    }
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root = document.RootElement;

                if (root.TryGetProperty(
                    "userId",
                    out JsonElement userId))
                {
                    HttpContext.Session.SetString(
                        "UserId",
                        userId.ToString());
                }

                if (root.TryGetProperty(
                    "email",
                    out JsonElement email))
                {
                    HttpContext.Session.SetString(
                        "Email",
                        email.GetString() ?? string.Empty);
                }

                string userRole = string.Empty;

                if (root.TryGetProperty(
                    "role",
                    out JsonElement role))
                {
                    userRole = role.GetString() ?? string.Empty;

                    HttpContext.Session.SetString(
                        "Role",
                        userRole);
                }

                TempData["SuccessMessage"] =
                    "Login successful.";

                // Organisers always go to their dashboard.
                if (string.Equals(
                    userRole,
                    "Organiser",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(
                        "Index",
                        "Organiser");
                }

                // Participants return to their selected event.
                if (string.Equals(
                    userRole,
                    "Participant",
                    StringComparison.OrdinalIgnoreCase)
                    && eventId > 0)
                {
                    return RedirectToAction(
                        "EventDetails",
                        "Home",
                        new { id = eventId.Value });
                }

                // Normal login goes to participant dashboard.
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Unable to connect to RaceDay API: {ex.Message}");

                return View(model);
            }
        }

        // -------------------------------------------------
        // LOGOUT
        // -------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _apiClient.SendAsync(
                    HttpMethod.Post,
                    "api/Auth/logout");
            }
            catch
            {
                // Local session is still cleared even if
                // the API is temporarily unavailable.
            }

            HttpContext.Session.Clear();

            TempData["SuccessMessage"] =
                "You have been logged out.";

            // Always return to public RaceDay landing page.
            return RedirectToAction("Index", "Home");
        }

        // -------------------------------------------------
        // API ERROR MESSAGE HELPER
        // -------------------------------------------------

        private static async Task<string> ReadApiMessageAsync(
            HttpResponseMessage response)
        {
            string content =
                await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return $"Request failed: {(int)response.StatusCode} " +
                       $"{response.StatusCode}.";
            }

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(content);

                if (document.RootElement.TryGetProperty(
                    "message",
                    out JsonElement message))
                {
                    return message.GetString()
                           ?? "The request could not be completed.";
                }
            }
            catch (JsonException)
            {
                // API did not return JSON.
            }

            return content;
        }
    }
}
