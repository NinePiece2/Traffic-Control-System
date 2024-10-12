using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;
using Traffic_Control_System.Services;

namespace Traffic_Control_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailService _emailService;
        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext applicationDbContext, IEmailService emailService)
        {
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
            _emailService = emailService;
        }
        public async Task<IActionResult> Index()
        {
            //var users = _userManager.Users.ToList();
            var numberOfPendingUsers = _applicationDbContext.PendingUserRequests.Count();

            ViewBag.NumberOfPendingUsers = numberOfPendingUsers;

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userID);
            TempData["UsersName"] = user.Name;

            return View();
        }

        public IActionResult GetNumberOfPendingUsers()
        {
            var numberOfPendingUsers = _applicationDbContext.PendingUserRequests.Count();

            return Json(numberOfPendingUsers);
        }

        public IActionResult AdminUsersList()
        {
            var userList = _userManager.Users
                .Where(u => u.EmailConfirmed)
                .ToList();

            var userListObjects = userList.Select(user =>
            {
                // Fetch user roles
                var roles = _userManager.GetRolesAsync(user).Result;

                // Check if the user has the Admin role
                if (!roles.Contains("Admin"))
                {
                    return null; // Skip this user if not an Admin
                }

                var userListObject = new UserList
                {
                    Id = user.Id,
                    EmailId = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = "Admin",
                    Name = user.Name
                };

                return userListObject; // Return the userListObject if valid
            }).Where(user => user != null) // Filter out any null values
            .ToList();

            return Json(new { result = userListObjects, count = userListObjects.Count});
        }

        public IActionResult UsersList()
        {
            var userList = _userManager.Users
                .Where(u => u.EmailConfirmed)
                .ToList();

            var userListObjects = userList.Select(user =>
            {
                // Fetch user roles
                var roles = _userManager.GetRolesAsync(user).Result;

                // Check if the user has the Admin role
                if (!roles.Contains("User"))
                {
                    return null; // Skip this user if not an Admin
                }

                var userListObject = new UserList
                {
                    Id = user.Id,
                    EmailId = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = "User",
                    Name = user.Name
                };

                return userListObject; // Return the userListObject if valid
            }).Where(user => user != null) // Filter out any null values
            .ToList();

            return Json(new { result = userListObjects, count = userListObjects.Count });
        }

        public IActionResult PendingUsersList()
        {
            var userList = _applicationDbContext.PendingUserRequests
                .ToList();

            var userListObjects = userList.Select(user =>
            {
                
                var userListObject = new UserList
                {
                    Id = user.Id,
                    EmailId = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = "User",
                    Name = user.Name
                };

                return userListObject; // Return the userListObject if valid
            }).Where(user => user != null) // Filter out any null values
            .ToList();

            return Json(new { result = userListObjects, count = userListObjects.Count });
        }

        public ActionResult RenderApprovalModal()
        {
            ApproveDenyModel modal = new ApproveDenyModel { ID = "ApprovalModal", textArea = false, cancelBtnMessage = "Cancel", confirmBtnMessage = "Approve", reminderText = "Are you sure you want to approve this user's access to the application?" };
            return PartialView("ApproveDenyModel", modal);
        }

        public ActionResult RenderDenyModal()
        {
            ApproveDenyModel modal = new ApproveDenyModel { ID = "DenyModal", textArea = false, cancelBtnMessage = "Cancel", confirmBtnMessage = "Deny", reminderText = "Are you sure you want to deny this user?", hintMessage = "This Will Delete Their Account." };
            return PartialView("ApproveDenyModel", modal);
        }
    }
}
