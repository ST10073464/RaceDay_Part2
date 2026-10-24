using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using System.Diagnostics;

namespace RaceDay.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RaceDayApiClient _apiClient;

        public HomeController(
            ILogger<HomeController> logger,
            RaceDayApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        // ---------------------------------------------------------
        // HOME / PARTICIPANT DASHBOARD
        // ---------------------------------------------------------

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");

            // User is not logged in.
            if (string.IsNullOrWhiteSpace(role))
            {
                return View(new List<EventViewModel>());
            }

            // For now the dashboard is specifically for participants.
            if (!string.Equals(
                    role,
                    "Participant",
                    StringComparison.OrdinalIgnoreCase))
            {
                return View(new List<EventViewModel>());
            }

            try
            {
                List<EventViewModel> events =
                    await _apiClient.GetEventsAsync();

                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load RaceDay events.");

                ViewBag.EventError =
                    "Events could not be loaded at this time.";

                return View(new List<EventViewModel>());
            }
        }
        // ---------------------------------------------------------
        // EVENT DETAILS
        // ---------------------------------------------------------

        public async Task<IActionResult> EventDetails(int id)
        {
            var role =
                HttpContext.Session.GetString("Role");

            if (!string.Equals(
                    role,
                    "Participant",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                EventViewModel? raceEvent =
                    await _apiClient.GetEventAsync(id);

                if (raceEvent == null)
                {
                    return NotFound();
                }

                List<CategoryViewModel> categories =
                    await _apiClient.GetEventCategoriesAsync(id);

                List<EnrolmentViewModel> enrolments =
                    await _apiClient.GetMyEnrolmentsAsync();

                EnrolmentViewModel? existingEnrolment =
                    enrolments.FirstOrDefault(
                        e => e.EventId == id);

                var model =
                    new EventDetailsViewModel
                    {
                        Event = raceEvent,
                        Categories = categories,
                        ExistingEnrolment = existingEnrolment
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load event {EventId}.",
                    id);

                return RedirectToAction(nameof(Index));
            }
        }

        // ------------------------------------------------------------
        // PARTICIPANT ENROLMENT
        // ------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            var role = HttpContext.Session.GetString("Role");

            // Only logged-in participants may enrol.
            if (!string.Equals(
                role,
                "Participant",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                var enrolment = new CreateEnrolmentViewModel
                {
                    EventId = eventId,
                    CategoryId = categoryId
                };

                var response = await _apiClient.SendAsync(
                    HttpMethod.Post,
                    "api/enrolments",
                    enrolment);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "You have successfully enrolled in the event.";

                    return RedirectToAction(
                        nameof(EventDetails),
                        new { id = eventId });
                }

                string errorMessage =
                    await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage =
                        $"Enrolment failed: {(int)response.StatusCode} {response.StatusCode}";
                }

                TempData["ErrorMessage"] = errorMessage;

                return RedirectToAction(
                    nameof(EventDetails),
                    new { id = eventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to enrol participant in event {EventId}, category {CategoryId}.",
                    eventId,
                    categoryId);

                TempData["ErrorMessage"] =
                    "Unable to complete the enrolment at this time.";

                return RedirectToAction(
                    nameof(EventDetails),
                    new { id = eventId });
            }
        }

        // ------------------------------------------------------------
        // MY ENROLMENTS
        // ------------------------------------------------------------

        public async Task<IActionResult> MyEnrolments()
        {
            var role = HttpContext.Session.GetString("Role");

            // Only logged-in participants may access this page.
            if (!string.Equals(
                role,
                "Participant",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                List<EnrolmentViewModel> enrolments =
                    await _apiClient.GetMyEnrolmentsAsync();

                return View(enrolments);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load participant enrolments.");

                ViewBag.EnrolmentError =
                    "Your enrolments could not be loaded at this time.";

                return View(new List<EnrolmentViewModel>());
            }
        }

        // ---------------------------------------------------------
        // PRIVACY
        // ---------------------------------------------------------

        public IActionResult Privacy()
        {
            return View();
        }

        // ---------------------------------------------------------
        // ERROR
        // ---------------------------------------------------------

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                });
        }
    }
}