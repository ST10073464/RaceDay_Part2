using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class ProfileController : Controller
    {
        private readonly RaceDayApiClient _apiClient;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            RaceDayApiClient apiClient,
            ILogger<ProfileController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ---------------------------------------------------------
        // VIEW CURRENT USER PROFILE
        // ---------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                ProfileViewModel? profile =
                    await _apiClient.GetProfileAsync();

                if (profile == null)
                {
                    TempData["ErrorMessage"] =
                        "Your profile could not be loaded.";

                    return RedirectToAction(
                        "Index",
                        "Home");
                }

                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load the user profile.");

                TempData["ErrorMessage"] =
                    "Your profile could not be loaded at this time.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }
        }

        // ---------------------------------------------------------
        // UPDATE CURRENT USER PROFILE
        // ---------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            ProfileViewModel model)
        {
            var userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "Index",
                    model);
            }

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.UpdateProfileAsync(model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Profile updated successfully.";

                    return RedirectToAction(
                        nameof(Index));
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Your profile could not be updated.");

                return View(
                    "Index",
                    model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to update the user profile.");

                ModelState.AddModelError(
                    string.Empty,
                    "Your profile could not be updated at this time.");

                return View(
                    "Index",
                    model);
            }
        }
        // ---------------------------------------------------------
        // UPLOAD CURRENT USER PROFILE PICTURE
        // ---------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(
            IFormFile profilePicture)
        {
            var userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (profilePicture == null ||
                profilePicture.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a profile picture.";

                return RedirectToAction(
                    nameof(Index));
            }

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.UploadProfilePictureAsync(
                        profilePicture);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Profile picture uploaded successfully.";

                    return RedirectToAction(
                        nameof(Index));
                }

                string apiError =
                    await response.Content.ReadAsStringAsync();

                _logger.LogWarning(
                    "Profile picture upload failed. API response: {ApiError}",
                    apiError);

                TempData["ErrorMessage"] =
                    "The profile picture could not be uploaded.";

                return RedirectToAction(
                    nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to upload the profile picture.");

                TempData["ErrorMessage"] =
                    "The profile picture could not be uploaded at this time.";

                return RedirectToAction(
                    nameof(Index));
            }
        }

    }
}