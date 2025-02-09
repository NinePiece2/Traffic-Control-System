using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Traffic_Control_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Traffic_Control_System.Services;
using Traffic_Control_System.Data;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Net;
using System;
using System.Security.Cryptography;


namespace Traffic_Control_System.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfiguration config;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService emailService;
        private readonly ApplicationDbContext _applicationDbContext;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, IEmailService _emailService, ApplicationDbContext applicationDbContext, IConfiguration _config)
        {
            _logger = logger;
            _userManager = userManager;
            emailService = _emailService;
            _applicationDbContext = applicationDbContext;
            config = _config;
        }

        public async Task<IActionResult> Index()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userID);

            if (user == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            TempData["UsersName"] = user.Name;

            return View();
        }

        public async Task<IActionResult> Video()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(config["VideoServiceURL"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var apiKey = WebUtility.UrlEncode(config["API_KEY"]);
                var param = $"userId={apiKey}";

                HttpResponseMessage response = await client.GetAsync($"Token/GetToken?{param}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var token = JsonConvert.DeserializeObject<string>(responseContent);
                    ViewBag.Token = token;
                }
                else
                {
                    ViewBag.Token = null;
                }
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult TrafficSignalsList()
        {
            var signalsList = _applicationDbContext.TrafficSignals.ToList();
            return Json(new { result = signalsList, count = signalsList.Count });
        }

        public IActionResult IncidentReportsList(int signalID)
        {
            var violations = _applicationDbContext.TrafficViolations
                .Where(v => v.ActiveSignalID == signalID)
                .Select(v => new TrafficViolation
                {
                    UID = v.UID,
                    DateCreated = v.DateCreated,
                    LicensePlate = v.LicensePlate
                })
                .ToList();

            return Json(new { result = violations, count = violations.Count });
        }

        public IActionResult IncidentReport(int ID)
        {
            if (ID == 0)
            {
                return NotFound();
            }

            var violation = new TrafficViolationsViewModel
            {
                ActiveSignalID = ID
            };

            return View(violation);
        }

        public JsonResult SaveTrafficSignal([FromBody] ActiveSignals trafficSignal)
        {
            if (trafficSignal == null || 
                string.IsNullOrWhiteSpace(trafficSignal.Address) || 
                string.IsNullOrWhiteSpace(trafficSignal.Direction1) || 
                string.IsNullOrWhiteSpace(trafficSignal.Direction2))
            {
                return Json(new { error = "Invalid input data." });
            }

            // Validate and convert GreenLight values
            if (!int.TryParse(trafficSignal.Direction1Green?.ToString(), out int green1) ||
                !int.TryParse(trafficSignal.Direction2Green?.ToString(), out int green2))
            {
                return Json(new { error = "Invalid green light times." });
            }
            
            trafficSignal.Direction1Green = green1;
            trafficSignal.Direction2Green = green2;

            StreamClients newstreamClient = new StreamClients();
            // Generate a unique DeviceStreamId
            newstreamClient.DeviceStreamID = Guid.NewGuid().ToString();

            // Generate a cryptographically secure, URL-safe API Key
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[64];
                rng.GetBytes(randomBytes);

                // Convert the byte array to a Base64 string
                string base64String = Convert.ToBase64String(randomBytes);

                // Make the Base64 string URL-safe
                newstreamClient.DeviceStreamKEY = base64String
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
            }
            

            // Save to the database
            _applicationDbContext.Add(newstreamClient);
            _applicationDbContext.SaveChanges();
            trafficSignal.DeviceStreamUID = newstreamClient.UID;
            trafficSignal.IsActive = true;
            _applicationDbContext.Add(trafficSignal);
            _applicationDbContext.SaveChanges();

            // Return the generated keys
            return Json(new {DeviceStreamID = newstreamClient.DeviceStreamID, APIKey = config["API_KEY"] });
        }

    }
}
