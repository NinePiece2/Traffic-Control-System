using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Traffic_Control_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Traffic_Control_System.Services;

namespace Traffic_Control_System.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService emailService;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, IEmailService _emailService)
        {
            _logger = logger;
            _userManager = userManager;
            emailService = _emailService;
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
