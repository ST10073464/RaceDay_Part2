using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiClient _apiClient;
        private readonly ILogger<OrganiserController> _logger;

        public OrganiserController(
            RaceDayApiClient apiClient,
            ILogger<OrganiserController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ORGANISER DASHBOARD
        public IActionResult Index()
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            return View();
        }

        // ORGANISER - MY EVENTS
        public async Task<IActionResult> MyEvents()
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                List<EventViewModel> events =
                    await _apiClient.GetMyEventsAsync();

                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load organiser events.");

                ViewBag.EventError =
                    "Your events could not be loaded at this time.";

                return View(new List<EventViewModel>());
            }
        }

        // ORGANISER - CREATE EVENT
        [HttpGet]
        public IActionResult CreateEvent()
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var model =
                new CreateEventViewModel
                {
                    EventDate = DateOnly.FromDateTime(
                        DateTime.Today.AddDays(1))
                };

            return View(model);
        }

        // ORGANISER - CREATE EVENT SUBMISSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(
            CreateEventViewModel model)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response =
                    await _apiClient.CreateEventAsync(model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event created successfully.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                string message =
                    await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    string.IsNullOrWhiteSpace(message)
                        ? "The event could not be created."
                        : message);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to create organiser event.");

                ModelState.AddModelError(
                    string.Empty,
                    "The event could not be created at this time.");

                return View(model);
            }
        }

        // ORGANISER - EDIT EVENT
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                EventViewModel? eventDetails =
                    await _apiClient.GetEventByIdAsync(id);

                if (eventDetails == null)
                {
                    TempData["ErrorMessage"] =
                        "The event could not be found.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                var model =
                    new EditEventViewModel
                    {
                        EventId = eventDetails.EventId,
                        EventName = eventDetails.EventName,
                        Description = eventDetails.Description,
                        EventDate = eventDetails.EventDate,
                        Location = eventDetails.Location,
                        DistanceKm = eventDetails.DistanceKm,
                        EventType = eventDetails.EventType,
                        BannerImageUrl = eventDetails.BannerImageUrl
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load event {EventId} for editing.",
                    id);

                TempData["ErrorMessage"] =
                    "The event could not be loaded at this time.";

                return RedirectToAction(
                    nameof(MyEvents));
            }
        }

        // ORGANISER - EDIT EVENT SUBMISSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(
            EditEventViewModel model)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response =
                    await _apiClient.UpdateEventAsync(
                        model.EventId,
                        model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event updated successfully.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "You are not authorised to edit this event.");

                    return View(model);
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "The event could not be found.");

                    return View(model);
                }

                string message =
                    await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    string.IsNullOrWhiteSpace(message)
                        ? "The event could not be updated."
                        : message);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to update event {EventId}.",
                    model.EventId);

                ModelState.AddModelError(
                    string.Empty,
                    "The event could not be updated at this time.");

                return View(model);
            }
        }

        // =========================================================
        // ORGANISER - MANAGE EVENT CATEGORIES
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ManageCategories(int eventId)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                EventViewModel? eventDetails =
                    await _apiClient.GetEventByIdAsync(eventId);

                if (eventDetails == null)
                {
                    TempData["ErrorMessage"] =
                        "The event could not be found.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                List<CategoryViewModel> categories =
                    await _apiClient.GetEventCategoriesAsync(
                        eventId);

                var model =
                    new ManageCategoriesViewModel
                    {
                        EventId = eventDetails.EventId,
                        EventName = eventDetails.EventName,
                        Categories = categories,
                        MinAge = 0,
                        MaxAge = 99,
                        DistanceKm = eventDetails.DistanceKm
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load categories for event {EventId}.",
                    eventId);

                TempData["ErrorMessage"] =
                    "The event categories could not be loaded.";

                return RedirectToAction(
                    nameof(MyEvents));
            }
        }


        // =========================================================
        // ORGANISER - CREATE CATEGORY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            ManageCategoriesViewModel model)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (model.MaxAge < model.MinAge)
            {
                ModelState.AddModelError(
                    nameof(model.MaxAge),
                    "Maximum age cannot be less than minimum age.");
            }

            if (!ModelState.IsValid)
            {
                EventViewModel? eventDetails =
                    await _apiClient.GetEventByIdAsync(
                        model.EventId);

                model.EventName =
                    eventDetails?.EventName ??
                    "RaceDay Event";

                model.Categories =
                    await _apiClient.GetEventCategoriesAsync(
                        model.EventId);

                return View(
                    "ManageCategories",
                    model);
            }

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.CreateCategoryAsync(
                        model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Category created successfully.";

                    return RedirectToAction(
                        nameof(ManageCategories),
                        new
                        {
                            eventId = model.EventId
                        });
                }

                string message =
                    await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    GetApiErrorMessage(
                        message,
                        "The category could not be created."));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to create category for event {EventId}.",
                    model.EventId);

                ModelState.AddModelError(
                    string.Empty,
                    "The category could not be created at this time.");
            }

            EventViewModel? currentEvent =
                await _apiClient.GetEventByIdAsync(
                    model.EventId);

            model.EventName =
                currentEvent?.EventName ??
                "RaceDay Event";

            model.Categories =
                await _apiClient.GetEventCategoriesAsync(
                    model.EventId);

            return View(
                "ManageCategories",
                model);
        }


        // =========================================================
        // ORGANISER - DELETE CATEGORY
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(
            int categoryId,
            int eventId)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.DeleteCategoryAsync(
                        categoryId);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Category deleted successfully.";
                }
                else if (
                    response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    TempData["ErrorMessage"] =
                        "This category cannot be deleted because participants are already enrolled in it.";
                }
                else if (
                    response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["ErrorMessage"] =
                        "You are not authorised to delete this category.";
                }
                else if (
                    response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] =
                        "The category could not be found.";
                }
                else
                {
                    string message =
                        await response.Content.ReadAsStringAsync();

                    TempData["ErrorMessage"] =
                        GetApiErrorMessage(
                            message,
                            "The category could not be deleted.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to delete category {CategoryId}.",
                    categoryId);

                TempData["ErrorMessage"] =
                    "The category could not be deleted at this time.";
            }

            return RedirectToAction(
                nameof(ManageCategories),
                new
                {
                    eventId
                });
        }

        // =========================================================
        // ORGANISER - RECORD RESULT
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> RecordResult(
            int eventId,
            int enrolmentId)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                EventViewModel? eventDetails =
                    await _apiClient.GetEventByIdAsync(
                        eventId);

                if (eventDetails == null)
                {
                    TempData["ErrorMessage"] =
                        "The event could not be found.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                List<EventEnrolmentViewModel> enrolments =
                    await _apiClient.GetEventEnrolmentsAsync(
                        eventId);

                EventEnrolmentViewModel? enrolment =
                    enrolments.FirstOrDefault(
                        e => e.EnrolmentId == enrolmentId);

                if (enrolment == null)
                {
                    TempData["ErrorMessage"] =
                        "The participant enrolment could not be found.";

                    return RedirectToAction(
                        nameof(EventParticipants),
                        new
                        {
                            eventId
                        });
                }

                if (!string.Equals(
                    enrolment.EnrolmentStatus,
                    "Confirmed",
                    StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] =
                        "A result can only be recorded for a confirmed enrolment.";

                    return RedirectToAction(
                        nameof(EventParticipants),
                        new
                        {
                            eventId
                        });
                }

                var model =
                    new RecordResultViewModel
                    {
                        EventId =
                            eventDetails.EventId,

                        EventName =
                            eventDetails.EventName,

                        EnrolmentId =
                            enrolment.EnrolmentId,

                        ParticipantName =
                            enrolment.ParticipantName,

                        CategoryName =
                            enrolment.CategoryName
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load enrolment {EnrolmentId} for result capture.",
                    enrolmentId);

                TempData["ErrorMessage"] =
                    "The result capture page could not be loaded.";

                return RedirectToAction(
                    nameof(EventParticipants),
                    new
                    {
                        eventId
                    });
            }
        }

        // =========================================================
        // ORGANISER - RECORD RESULT SUBMISSION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordResult(
            RecordResultViewModel model)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (model.FinishTime <= TimeSpan.Zero)
            {
                ModelState.AddModelError(
                    nameof(model.FinishTime),
                    "Finish time must be greater than zero.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.RecordResultAsync(
                        model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Result recorded successfully.";

                    return RedirectToAction(
                        nameof(EventResults),
                        new
                        {
                            eventId = model.EventId
                        });
                }

                string message =
                    await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    string.Empty,
                    GetApiErrorMessage(
                        message,
                        "The result could not be recorded."));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to record result for enrolment {EnrolmentId}.",
                    model.EnrolmentId);

                ModelState.AddModelError(
                    string.Empty,
                    "The result could not be recorded at this time.");
            }

            return View(model);
        }

        // =========================================================
        // ORGANISER - EVENT RESULTS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EventResults(
            int eventId)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                EventViewModel? eventDetails =
                    await _apiClient.GetEventByIdAsync(
                        eventId);

                if (eventDetails == null)
                {
                    TempData["ErrorMessage"] =
                        "The event could not be found.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                List<ResultViewModel> results =
                    await _apiClient.GetEventResultsAsync(
                        eventId);

                var model =
                    new EventResultsViewModel
                    {
                        EventId =
                            eventDetails.EventId,

                        EventName =
                            eventDetails.EventName,

                        EventDate =
                            eventDetails.EventDate,

                        Location =
                            eventDetails.Location,

                        Results =
                            results
                                .OrderBy(
                                    r => r.FinishTime)
                                .ToList()
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load results for event {EventId}.",
                    eventId);

                TempData["ErrorMessage"] =
                    "The event results could not be loaded.";

                return RedirectToAction(
                    nameof(MyEvents));
            }
        }

        // =========================================================
        // ORGANISER - EVENT PARTICIPANTS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EventParticipants(int eventId)
        {
            var role =
                HttpContext.Session.GetString("Role");

            // Only organisers may access this page.
            if (!string.Equals(
                role,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                // Load the event.
                EventViewModel? eventDetails =
                    await _apiClient.GetEventByIdAsync(eventId);

                if (eventDetails == null)
                {
                    TempData["ErrorMessage"] =
                        "The event could not be found.";

                    return RedirectToAction(
                        nameof(MyEvents));
                }

                // Load enrolments for this event.
                List<EventEnrolmentViewModel> enrolments =
                    await _apiClient.GetEventEnrolmentsAsync(
                        eventId);

                var model =
                    new EventParticipantsViewModel
                    {
                        EventId =
                            eventDetails.EventId,

                        EventName =
                            eventDetails.EventName,

                        EventDate =
                            eventDetails.EventDate,

                        Location =
                            eventDetails.Location,

                        Enrolments =
                            enrolments
                                .OrderBy(
                                    e => e.ParticipantName)
                                .ToList()
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load participants for event {EventId}.",
                    eventId);

                TempData["ErrorMessage"] =
                    "The event participants could not be loaded.";

                return RedirectToAction(
                    nameof(MyEvents));
            }
        }

        // =========================================================
        // API ERROR MESSAGE HELPER
        // =========================================================
        private static string GetApiErrorMessage(
            string responseContent,
            string fallbackMessage)
        {
            if (string.IsNullOrWhiteSpace(
                responseContent))
            {
                return fallbackMessage;
            }

            try
            {
                using System.Text.Json.JsonDocument document =
                    System.Text.Json.JsonDocument.Parse(
                        responseContent);

                if (document.RootElement.TryGetProperty(
                    "message",
                    out System.Text.Json.JsonElement messageElement))
                {
                    string? message =
                        messageElement.GetString();

                    if (!string.IsNullOrWhiteSpace(
                        message))
                    {
                        return message;
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Fall back to the supplied friendly message.
            }

            return fallbackMessage;
        }

        // ---------------------------------------------------------
        // UPLOAD EVENT BANNER
        // ---------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadEventBanner(
            int eventId,
            IFormFile? bannerImage)
        {
            string? userRole =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                userRole,
                "Organiser",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (bannerImage == null ||
                bannerImage.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select an event banner image.";

                return RedirectToAction(
                    nameof(EditEvent),
                    new { id = eventId });
            }

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.UploadEventBannerAsync(
                        eventId,
                        bannerImage);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event banner uploaded successfully.";
                }
                else
                {
                    string error =
                        await response.Content.ReadAsStringAsync();

                    _logger.LogWarning(
                        "Event banner upload failed. API response: {Error}",
                        error);

                    TempData["ErrorMessage"] =
                        "The event banner could not be uploaded.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while uploading the event banner.");

                TempData["ErrorMessage"] =
                    "An unexpected error occurred while uploading the event banner.";
            }

            return RedirectToAction(
                nameof(EditEvent),
                new { id = eventId });
        }
    }
}