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
        // REGISTER
        // -------------------------------------------------

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
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

                    return RedirectToAction(nameof(Login));
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
        // LOGIN
        // -------------------------------------------------

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model)
        {
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

                // Capture the authentication cookie returned by RaceDay.API.
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

                if (root.TryGetProperty("userId", out JsonElement userId))
                {
                    HttpContext.Session.SetString(
                        "UserId",
                        userId.ToString());
                }

                if (root.TryGetProperty("email", out JsonElement email))
                {
                    HttpContext.Session.SetString(
                        "Email",
                        email.GetString() ?? string.Empty);
                }

                if (root.TryGetProperty("role", out JsonElement role))
                {
                    HttpContext.Session.SetString(
                        "Role",
                        role.GetString() ?? string.Empty);
                }

                TempData["SuccessMessage"] =
                    "Login successful.";

                if (string.Equals(
                     role.GetString(),
                     "Organiser",
                     StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(
                        "Index",
                        "Organiser");
                }

                return RedirectToAction(
                    "Index",
                    "Home");


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

            return RedirectToAction(
                nameof(Login));
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