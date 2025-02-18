using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Policy;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;
using Traffic_Control_System.Services;
using Microsoft.AspNetCore.SignalR;
using Traffic_Control_System.Hubs;
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
        private readonly IHubContext<ControlHub> _hubContext;
        private readonly IVideoService videoService;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, IEmailService _emailService, ApplicationDbContext applicationDbContext, 
            IConfiguration _config, IVideoService videoService, IHubContext<ControlHub> hubContext)
        {
            _logger = logger;
            _userManager = userManager;
            emailService = _emailService;
            _applicationDbContext = applicationDbContext;
            config = _config;
            _hubContext = hubContext;
            this.videoService = videoService;
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

        public IActionResult Stream(string ID)
        {
            if (ID == null)
            {
                return BadRequest("ID Can Not be Null");
            }

            int IDint = Int32.Parse(ID);
            var device = _applicationDbContext.StreamClients
                .Where(s => s.UID == _applicationDbContext.TrafficSignals
                    .Where(ts => ts.DeviceStreamUID == IDint)
                    .Select(ts => ts.DeviceStreamUID)
                    .FirstOrDefault())
                .Select(s => s.DeviceStreamID)
                .FirstOrDefault();


            var viewModel = new VideStreamViewModel
            {
                VideoURL = $"/VideoServiceProxy/Stream/{device}/output.m3u8"
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Report(string ID)
        {
            if (ID == null)
            {
                return BadRequest("ID Can Not be Null");
            }

            int IDint = Int32.Parse(ID);
            var violation = _applicationDbContext.TrafficViolations
                .Where(s => s.UID == IDint)
                .FirstOrDefault();

            var device = _applicationDbContext.StreamClients
                .Where(s => s.UID == _applicationDbContext.TrafficSignals
                    .Where(ts => ts.DeviceStreamUID == violation.ActiveSignalID)
                    .Select(ts => ts.DeviceStreamUID)
                    .FirstOrDefault())
                .Select(s => s.DeviceStreamID)
                .FirstOrDefault();
            
            var folderName = violation.Filename.Replace(".mp4", "").Replace(" ", "-");

            var viewModel = new ReportViewModel
            {
                VideoURL = $"/VideoServiceProxy/Clip/GetFile/{device}/{folderName}/hls/playlist.m3u8",
                DateCreated = violation.DateCreated,
                LicensePlate = violation.LicensePlate
            };
            return View(viewModel);
        }

        [HttpGet("VideoServiceProxy/{*url}")]
        public async Task<IActionResult> VideoServiceProxy(string url)
        {
            try
            {
                var httpResponse = await videoService.VideoServiceProxy(url);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var contentStream = await httpResponse.Content.ReadAsStreamAsync();

                    var contentType = httpResponse.Content.Headers.ContentType.ToString();

                    return new FileStreamResult(contentStream, contentType);
                }
                else
                {
                    return BadRequest(httpResponse);
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult TrafficSignalsList()
        {
            var userList = _applicationDbContext.TrafficSignals
                .ToList();
            
            return Json(new { result = userList, count = userList.Count });


        }


            public IActionResult IncidentReportsList(int signalID)
            {
                // Query the TrafficViolations table based on the ActiveSignalID
                var violations = _applicationDbContext.TrafficViolations
                    .Where(v => v.ActiveSignalID == signalID)
                    .Select(v => new TrafficViolation
                    {
                        UID = v.UID,
                        DateCreated = v.DateCreated,
                        LicensePlate = v.LicensePlate
                    })
                    .ToList();

                // Return the result as JSON with the count
                return Json(new { result = violations, count = violations.Count });
            }



        public IActionResult IncidentReport(int ID)
        {
            
            if (ID == null)
            {

                return NotFound(); // Or handle the case where the report isn't found
            }

            var violation = new TrafficViolationsViewModel{
                ActiveSignalID=ID
            };

            // Pass the data to the view (or you can create a ViewModel)
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

        public IActionResult SignalRTest() { return View(); }

        [HttpPost]
        public async Task<IActionResult> UpdateTrafficSignal([FromBody] ActiveSignals signal)
        {
            try
            {
                // Validate that the ID is an integer and greater than 0
                if (signal.ID <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid Signal ID." });
                }

                // Fetch from ActiveSignals table
                var existingSignal = _applicationDbContext.ActiveSignals.FirstOrDefault(s => s.ID == signal.ID);

                if (existingSignal == null)
                {
                    return NotFound(new { success = false, message = "Traffic signal not found." });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(signal.Address))
                {
                    return BadRequest(new { success = false, message = "Address is required." });
                }
                if (string.IsNullOrWhiteSpace(signal.Direction1) || string.IsNullOrWhiteSpace(signal.Direction2))
                {
                    return BadRequest(new { success = false, message = "Both directions are required." });
                }

                // Ensure Direction1Green and Direction2Green are valid integers and non-negative
                if (signal.Direction1Green < 0 || signal.Direction2Green < 0)
                {
                    return BadRequest(new { success = false, message = "Green signal times must be non-negative integers." });
                }

                // Update existing ActiveSignals entry
                existingSignal.Address = signal.Address;
                existingSignal.Direction1 = signal.Direction1;
                existingSignal.Direction2 = signal.Direction2;
                existingSignal.Direction1Green = signal.Direction1Green;
                existingSignal.Direction2Green = signal.Direction2Green;

                // Update the entity in the database
                _applicationDbContext.ActiveSignals.Update(existingSignal);
                await _applicationDbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Traffic signal updated successfully!" });
            }
            catch (Exception ex)
            {
                // Log the exception for better debugging
                _logger.LogError(ex, "An error occurred while updating the traffic signal.");

                return StatusCode(500, new { success = false, message = "An error occurred while saving the entity changes. Please check the inner exception for details." });
            }
        }
    }
}
