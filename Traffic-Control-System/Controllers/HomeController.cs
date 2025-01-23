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
        private readonly IVideoService videoService;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, IEmailService _emailService, ApplicationDbContext applicationDbContext, 
            IConfiguration _config, IVideoService videoService)
        {
            _logger = logger;
            _userManager = userManager;
            emailService = _emailService;
            _applicationDbContext = applicationDbContext;
            config = _config;
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

            var viewModel = new ReportViewModel
            {
                VideoURL = $"/VideoServiceProxy/Clip/GetFile/{device}/hls/playlist.m3u8",
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

    }
}
