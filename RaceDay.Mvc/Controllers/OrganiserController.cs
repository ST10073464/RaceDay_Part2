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


        // Ownership is checked against the authenticated organiser's own event list.
        // This fails closed when the API cannot verify ownership.
        private bool IsOrganiser() =>
            string.Equals(HttpContext.Session.GetString("Role"),
                "Organiser", StringComparison.OrdinalIgnoreCase);

        private async Task<bool> OwnsEventAsync(int eventId)
        {
            if (eventId <= 0) return false;
            try
            {
                var ownedEvents = await _apiClient.GetMyEventsAsync();
                return ownedEvents.Any(e => e.EventId == eventId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not verify event ownership for event {EventId}.", eventId);
                return false;
            }
        }

        // READ-ONLY LIST OF EVENTS FROM ALL ORGANISERS
        [HttpGet]
        public async Task<IActionResult> AllEvents()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            try
            {
                var events = await _apiClient.GetEventsAsync();
                return View(events.OrderBy(e => e.EventDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to load all events.");
                TempData["ErrorMessage"] = "Events could not be loaded.";
                return View(new List<EventViewModel>());
            }
        }

        // ORGANISER DASHBOARD
        public async Task<IActionResult> Index()
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

            List<EventViewModel> events =
                await _apiClient.GetMyEventsAsync();

            var dashboard =
                new OrganiserDashboardViewModel();

            DateOnly today =
                DateOnly.FromDateTime(DateTime.Today);

            foreach (EventViewModel raceEvent in events)
            {
                List<EventEnrolmentViewModel> enrolments;

                try
                {
                    enrolments =
                        await _apiClient.GetEventEnrolmentsAsync(
                            raceEvent.EventId);
                }
                catch (HttpRequestException)
                {
                    enrolments =
                        new List<EventEnrolmentViewModel>();
                }

                bool isUpcoming =
                    raceEvent.EventDate >= today;

                dashboard.Events.Add(
                    new OrganiserDashboardEventViewModel
                    {
                        EventId = raceEvent.EventId,
                        EventName = raceEvent.EventName,
                        EventDate = raceEvent.EventDate,
                        Location = raceEvent.Location,
                        EventType = raceEvent.EventType,
                        TotalEnrolments = enrolments.Count,
                        IsUpcoming = isUpcoming
                    });
            }

            dashboard.Events =
                dashboard.Events
                    .OrderBy(e => e.EventDate)
                    .ToList();

            dashboard.TotalEvents =
                dashboard.Events.Count;

            dashboard.UpcomingEvents =
                dashboard.Events.Count(
                    e => e.IsUpcoming);

            dashboard.TotalEnrolments =
                dashboard.Events.Sum(
                    e => e.TotalEnrolments);

            return View(dashboard);
        }

        // ORGANISER - MY EVENTS
        // Load categories and enrolments for each event tile.
        public async Task<IActionResult> MyEvents()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            try
            {
                List<EventViewModel> events = await _apiClient.GetMyEventsAsync();
                var eventCategories = new Dictionary<int, List<CategoryViewModel>>();
                var eventEnrolments = new Dictionary<int, List<EventEnrolmentViewModel>>();

                foreach (var raceEvent in events)
                {
                    // A failure to load one event's supplementary information
                    // must not hide all of the organiser's events.
                    try
                    {
                        eventCategories[raceEvent.EventId] =
                            await _apiClient.GetEventCategoriesAsync(raceEvent.EventId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Unable to load categories for event {EventId}.", raceEvent.EventId);
                        eventCategories[raceEvent.EventId] = new List<CategoryViewModel>();
                    }

                    try
                    {
                        eventEnrolments[raceEvent.EventId] =
                            await _apiClient.GetEventEnrolmentsAsync(raceEvent.EventId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Unable to load enrolments for event {EventId}.", raceEvent.EventId);
                        // Null means unknown, NOT zero. The view must not
                        // incorrectly claim the event is safe to delete.
                        eventEnrolments[raceEvent.EventId] = null!;
                    }
                }

                ViewBag.EventCategories = eventCategories;
                ViewBag.EventEnrolments = eventEnrolments;
                return View(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to load organiser events.");
                ViewBag.EventError = "Your events could not be loaded.";
                ViewBag.EventCategories = new Dictionary<int, List<CategoryViewModel>>();
                ViewBag.EventEnrolments = new Dictionary<int, List<EventEnrolmentViewModel>>();
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
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            // Do not allow new events to be scheduled in the past.
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (model.EventDate < today)
            {
                ModelState.AddModelError(
                    nameof(model.EventDate),
                    "Event date cannot be in the past. Select today or a future date.");
            }

            if (model.BannerImage != null)
            {
                if (!string.IsNullOrWhiteSpace(model.BannerImageUrl))
                {
                    ModelState.AddModelError(
                        nameof(model.BannerImage),
                        "Choose either an image URL or a computer file, not both.");
                }

                string extension = Path.GetExtension(
                    model.BannerImage.FileName).ToLowerInvariant();

                if (model.BannerImage.Length == 0 ||
                    model.BannerImage.Length > 5 * 1024 * 1024 ||
                    !new[] { ".jpg", ".jpeg", ".png", ".webp" }
                        .Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.BannerImage),
                        "Select a JPG, JPEG, PNG or WEBP image of 5 MB or less.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var response =
                    await _apiClient.CreateEventAsync(model);

                if (!response.IsSuccessStatusCode)
                {
                    string message =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        string.IsNullOrWhiteSpace(message)
                            ? "The event could not be created."
                            : GetApiErrorMessage(
                                message,
                                "The event could not be created."));

                    return View(model);
                }

                // Read the event ID for every successful creation,
                // regardless of whether a banner was supplied.
                int eventId = 0;

                string json =
                    await response.Content.ReadAsStringAsync();

                try
                {
                    using var document =
                        System.Text.Json.JsonDocument.Parse(json);

                    if (document.RootElement.TryGetProperty(
                        "eventId", out var idElement))
                    {
                        idElement.TryGetInt32(out eventId);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Event created but the API response was not valid JSON.");
                }

                if (eventId <= 0)
                {
                    TempData["SuccessMessage"] =
                        "Event created, but its ID could not be read. " +
                        "Open the event from My Events to add categories.";

                    return RedirectToAction(nameof(MyEvents));
                }

                string successMessage =
                    "Event created successfully. " +
                    "You can now add categories for participants.";

                // Upload the banner if one was selected.
                if (model.BannerImage != null)
                {
                    try
                    {
                        using var uploadResponse =
                            await _apiClient.UploadEventBannerAsync(
                                eventId,
                                model.BannerImage);

                        if (!uploadResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning(
                                "Banner upload failed for event {EventId}: {Status}",
                                eventId,
                                uploadResponse.StatusCode);

                            successMessage =
                                "Event created successfully, but the banner " +
                                "upload failed. You can retry from Edit Event.";
                        }
                    }
                    catch (Exception uploadException)
                    {
                        _logger.LogError(
                            uploadException,
                            "Banner upload failed for event {EventId}",
                            eventId);

                        successMessage =
                            "Event created successfully, but the banner " +
                            "upload failed. You can retry from Edit Event.";
                    }
                }

                TempData["SuccessMessage"] = successMessage;

                // Continue to category setup for the new event.
                return RedirectToAction(
                    nameof(ManageCategories),
                    new
                    {
                        eventId = eventId,
                        returnTo = "EditEvent"
                    });
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
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(id))
                return NotFound();

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
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(model.EventId))
                return NotFound();

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
                        nameof(EditEvent),
                        new { id = model.EventId });
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
        // ORGANISER - DELETE EVENT
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

            try
            {
                // Never delete if enrolments cannot be verified.
                var enrolments =
                    await _apiClient.GetEventEnrolmentsAsync(eventId);

                if (enrolments == null)
                {
                    TempData["ErrorMessage"] =
                        "Deletion is unavailable because participant enrolments could not be verified.";

                    return RedirectToAction(
                        nameof(EditEvent),
                        new { id = eventId });
                }

                if (enrolments.Count > 0)
                {
                    TempData["ErrorMessage"] =
                        "This event cannot be deleted because participants are already enrolled.";

                    return RedirectToAction(
                        nameof(EditEvent),
                        new { id = eventId });
                }

                using var response =
                    await _apiClient.DeleteEventAsync(eventId);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event deleted successfully.";

                    return RedirectToAction(nameof(MyEvents));
                }

                if (response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    TempData["ErrorMessage"] =
                        "This event cannot be deleted because participants are already enrolled.";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["ErrorMessage"] =
                        "You are not authorised to delete this event.";
                }
                else if (response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] =
                        "The event could not be found.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "The event could not be deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to delete event {EventId}.",
                    eventId);

                TempData["ErrorMessage"] =
                    "Deletion could not be completed. Please try again.";
            }

            return RedirectToAction(
                nameof(EditEvent),
                new { id = eventId });
        }



        // =========================================================
        // ORGANISER - MANAGE EVENT CATEGORIES
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ManageCategories(
            int eventId,
            string? returnTo = null)
        {
            ViewBag.ReturnTo =
                string.Equals(returnTo, "EventParticipants",
                    StringComparison.OrdinalIgnoreCase)
                    ? "EventParticipants"
                    : "EditEvent";

            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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
            ManageCategoriesViewModel model,
            string? returnTo = null)
        {
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(model.EventId))
                return NotFound();

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
                            eventId = model.EventId,
                            returnTo
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
            int eventId,
            string? returnTo = null)
        {
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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
                    eventId,
                    returnTo
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
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(model.EventId))
                return NotFound();

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
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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
        // ORGANISER - ALL PARTICIPANTS ACROSS OWN EVENTS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> AllParticipants()
        {
            if (!IsOrganiser())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                List<EventViewModel> events =
                    await _apiClient.GetMyEventsAsync();

                var model = new AllParticipantsViewModel
                {
                    TotalEvents = events.Count
                };

                foreach (EventViewModel raceEvent in events)
                {
                    List<EventEnrolmentViewModel> enrolments =
                        await _apiClient.GetEventEnrolmentsAsync(
                            raceEvent.EventId);

                    foreach (EventEnrolmentViewModel enrolment in enrolments)
                    {
                        model.Enrolments.Add(
                            new AllParticipantEnrolmentViewModel
                            {
                                EventId = raceEvent.EventId,
                                EventName = raceEvent.EventName,
                                Enrolment = enrolment
                            });
                    }
                }

                model.Enrolments = model.Enrolments
                    .OrderByDescending(e => e.Enrolment.EnrolmentDate)
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to load all organiser participants.");

                TempData["ErrorMessage"] =
                    "The participant records could not be loaded.";

                return RedirectToAction(nameof(Index));
            }
        }



        // =========================================================
        // ORGANISER - EVENT PARTICIPANTS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> EventParticipants(
            int eventId,
            string? returnTo = null)
        {
            ViewBag.ReturnTo = returnTo;

            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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

                // Only permit the two known navigation destinations.
                ViewBag.ParticipantsReturnTo =
                    string.Equals(returnTo, "EditEvent", StringComparison.OrdinalIgnoreCase)
                        ? "EditEvent"
                        : "Dashboard";

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
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEventBannerUrl(
            int eventId,
            string bannerUrl)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

            if (!Uri.TryCreate(
                    bannerUrl?.Trim(),
                    UriKind.Absolute,
                    out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                TempData["ErrorMessage"] =
                    "Enter a valid HTTPS image URL.";

                return RedirectToAction(
                    nameof(EditEvent),
                    new { id = eventId });
            }

            try
            {
                var existingEvent =
                    await _apiClient.GetEventByIdAsync(eventId);

                if (existingEvent == null)
                    return NotFound();

                var model = new EditEventViewModel
                {
                    EventId = existingEvent.EventId,
                    EventName = existingEvent.EventName,
                    Description = existingEvent.Description,
                    EventDate = existingEvent.EventDate,
                    Location = existingEvent.Location,
                    DistanceKm = existingEvent.DistanceKm,
                    EventType = existingEvent.EventType,
                    BannerImageUrl = uri.ToString()
                };

                using var response =
                    await _apiClient.UpdateEventAsync(eventId, model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event banner URL saved successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "The banner URL could not be saved.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to save banner URL for event {EventId}.",
                    eventId);

                TempData["ErrorMessage"] =
                    "An error occurred while saving the banner URL.";
            }

            return RedirectToAction(
                nameof(EditEvent),
                new { id = eventId });
        }


        // ---------------------------------------------------------
        // REMOVE EVENT BANNER
        // ---------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEventBanner(
            int eventId)
        {
            // Reject direct URL access and forged submissions for events owned by another organiser.
            if (!IsOrganiser())
                return RedirectToAction("Login", "Account");

            if (!await OwnsEventAsync(eventId))
                return NotFound();

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

            try
            {
                using HttpResponseMessage response =
                    await _apiClient.RemoveEventBannerAsync(
                        eventId);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "Event banner removed successfully.";
                }
                else
                {
                    string error =
                        await response.Content.ReadAsStringAsync();

                    _logger.LogWarning(
                        "Event banner removal failed. API response: {Error}",
                        error);

                    TempData["ErrorMessage"] =
                        "The event banner could not be removed.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while removing the event banner.");

                TempData["ErrorMessage"] =
                    "An unexpected error occurred while removing the event banner.";
            }

            return RedirectToAction(
                nameof(EditEvent),
                new { id = eventId });
        }
    }
}