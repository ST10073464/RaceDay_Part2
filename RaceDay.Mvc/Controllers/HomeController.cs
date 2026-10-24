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

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ApiTest()
        {
            try
            {
                var response = await _apiClient.SendAsync(
                    HttpMethod.Get,
                    "api/access-test/protected");

                return Content(
                    $"RaceDay API responded with: {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return Content(
                    $"API connection failed: {ex.Message}");
            }
        }

        public IActionResult SessionTest()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var email = HttpContext.Session.GetString("Email");
            var role = HttpContext.Session.GetString("Role");
            var apiCookie = HttpContext.Session.GetString("ApiCookie");

            return Content(
                $"UserId: {userId ?? "NOT FOUND"}\n" +
                $"Email: {email ?? "NOT FOUND"}\n" +
                $"Role: {role ?? "NOT FOUND"}\n" +
                $"ApiCookie: {(string.IsNullOrWhiteSpace(apiCookie) ? "NOT FOUND" : "STORED")}");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}