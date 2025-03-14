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
using static Traffic_Control_System.FilterHelper;
using System.Linq.Expressions;

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
            
            var signal = _applicationDbContext.TrafficSignals
                .Where(ts => ts.DeviceStreamUID == Int32.Parse(ID))
                .FirstOrDefault();

            var device = _applicationDbContext.StreamClients
                .Where(s => s.UID == signal.DeviceStreamUID)
                .Select(s => s.DeviceStreamID)
                .FirstOrDefault();

            var clientConnected = _applicationDbContext.SignalRClient
                .Where(s => s.ActiveSignalID == signal.DeviceStreamUID && s.ClientType == "Python")
                .Count() > 0;

            var viewModel = new VideStreamViewModel
            {
                VideoURL = $"/VideoServiceProxy/Stream/{device}/output.m3u8",
                Intersection = signal.Address,
                Direction1 = signal.Direction1,
                Direction2 = signal.Direction2,
                Direction1GreenTime = signal.Direction1Green,
                Direction2GreenTime = signal.Direction2Green,
                IsClientConnected = clientConnected
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

        [HttpPost]
        public IActionResult TrafficSignalsList([FromBody] PagingQueryModel pagingModel)
        {
            var query = _applicationDbContext.TrafficSignals.AsEnumerable();

            // Then apply the filter:
            if (pagingModel.Where != null && pagingModel.Where.Count > 0)
            {
                var filterExpression = ApplyFilters<TrafficSignals>(pagingModel.Where, "and").Compile();
                query = query.Where(filterExpression);
            }

            var query2 = query.AsQueryable();

            // Apply sorting if provided
            if (pagingModel.Sorted != null && pagingModel.Sorted.Any())
            {
                foreach (var sort in pagingModel.Sorted)
                {
                    var parameter = Expression.Parameter(typeof(TrafficSignals), "s");
                    var property = Expression.Property(parameter, sort.Name);
                    var lambda = Expression.Lambda(property, parameter);

                    string methodName = sort.Direction == "ascending" ? "OrderBy" : "OrderByDescending";
                    var methodCallExpression = Expression.Call(
                        typeof(Queryable),
                        methodName,
                        new Type[] { typeof(TrafficSignals), property.Type },
                        query2.Expression,
                        lambda
                    );

                    query2 = query2.Provider.CreateQuery<TrafficSignals>(methodCallExpression);
                }
            }
            else
            {
                // Default sorting if none is provided
                query = query.OrderBy(s => s.ID);
            }

            int totalCount = query2.Count();

            var signals = query2.Skip(pagingModel.Skip).Take(pagingModel.Take).ToList();

            return Json(new { result = signals, count = totalCount });
        }



        [HttpPost]
        public IActionResult IncidentReportsList(int signalID, [FromBody] PagingQueryModel pagingModel)
        {
            var query = _applicationDbContext.TrafficViolations
                .Where(v => v.ActiveSignalID == signalID)
                .OrderByDescending(v => v.DateCreated)
                .AsEnumerable();

            // Then apply the filter:
            if (pagingModel.Where != null && pagingModel.Where.Count > 0)
            {
                var filterExpression = ApplyFilters<TrafficViolations>(pagingModel.Where, "and").Compile();
                query = query.Where(filterExpression);
            }

            int totalCount = query.Count();

            var violations = query.Skip(pagingModel.Skip).Take(pagingModel.Take)
                .Select(v => new TrafficViolation
                {
                    UID = v.UID,
                    DateCreated = v.DateCreated,
                    LicensePlate = v.LicensePlate
                })
                .ToList();

            return Json(new { result = violations, count = totalCount });
        }

        public IActionResult IncidentReport(int ID)
        {
            
            if (ID == null)
            {

                return NotFound();
            }

            var violation = new TrafficViolationsViewModel{
                ActiveSignalID=ID
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
                !int.TryParse(trafficSignal.Direction2Green?.ToString(), out int green2) ||
                !int.TryParse(trafficSignal.PedestrianWalkTime?.ToString(), out int walkTime))
            {
                return Json(new { error = "Invalid green light times." });
            }
            
            trafficSignal.Direction1Green = green1;
            trafficSignal.Direction2Green = green2;
            trafficSignal.PedestrianWalkTime = walkTime;

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

            var output = new{
                DeviceStreamID = newstreamClient.DeviceStreamID, 
                APIKey = config["API_KEY"],
                StreamURL = config["VideoServiceURL"].Replace("https://", "rtmp://") + "/live/",
                ApiURL = config["APIURL"] + "/",
                VideoURL = config["VideoServiceURL"] + "/",
                MvcURL = config["BaseUrl"] + "/"
            };

            // Return the generated keys
            return Json(output);
        }
        
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

                if (signal.PedestrianWalkTime < 0 )
                {
                    return BadRequest(new { success = false, message = "Pedestrian Walk Time must be non-negative integers." });
                }

                // Update existing ActiveSignals entry
                existingSignal.Address = signal.Address;
                existingSignal.Direction1 = signal.Direction1;
                existingSignal.Direction2 = signal.Direction2;
                existingSignal.Direction1Green = signal.Direction1Green;
                existingSignal.Direction2Green = signal.Direction2Green;
                existingSignal.PedestrianWalkTime = signal.PedestrianWalkTime;
                existingSignal.BuzzerVolume = signal.BuzzerVolume;

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

        public ActionResult RenderPopUpModel()
        {
            PopUpModel modal = new PopUpModel { ID = "PopUpModel", textArea = false, cancelBtnMessage = "", confirmBtnMessage = "Okay", reminderText = "No Traffic Light client has been configured for this light." };
            return PartialView("~/Views/Shared/PopUpModel.cshtml", modal);
        }
    }
}
