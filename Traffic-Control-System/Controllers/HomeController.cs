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
using Traffic_Control_System.Data;

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
                    var responseContent = response.Content.ReadAsStringAsync().Result;
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

        public IActionResult ActiveSignalsList()
        {
            var userList = _applicationDbContext.ActiveSignals
                .ToList();
            
            return Json(new { result = userList, count = userList.Count });


        }
    }
}
