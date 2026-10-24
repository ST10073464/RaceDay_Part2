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
    }
}