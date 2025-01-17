using Azure.Core;
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
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext applicationDbContext, IEmailService emailService, IConfiguration configuration, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
            _emailService = emailService;
            _configuration = configuration;
            _roleManager = roleManager;
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
                .Where(u => u.AccountApproved)
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

            return Json(new { result = userListObjects, count = userListObjects.Count });
        }

        public IActionResult UsersList()
        {
            var userList = _userManager.Users
                .Where(u => u.AccountApproved)
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

        public async Task<IActionResult> ApproveUser(string userId)
        {
            try
            {
                var existingUser = await _userManager.FindByIdAsync(userId);
                var homeUrl = _configuration["BaseUrl"];

                var htmlEmailBody =
                        $@"
                        <!DOCTYPE html>
                        <html lang=""en"">
                        <head>
                            <meta charset=""UTF-8"">
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Account Created</title>
                        </head>
                        <body>
                            <h2>Traffic Control System - Your Account Has Been Approved</h2>
                            <p>
                                Congratulations! Your account has been Approved.
                            </p>
                            <p>
                            <p>
                                Please click the following link to log in:
                                <a href='{homeUrl}'>Login to Your Account</a>
                            </p>
                        </body>
                        </html>";

                await _emailService.SendEmail(existingUser.Email, "Traffic Control System - Your Account Has Been Approved", false, htmlEmailBody, false, null, null);

                existingUser.AccountApproved = true;
                await _userManager.UpdateAsync(existingUser);

                return Ok("User approved successfully!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> DenyUser(string userId)
        {
            var existingUser = await _userManager.FindByIdAsync(userId);
            if (existingUser != null)
            {

                var existingRoles = await _userManager.GetRolesAsync(existingUser);
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(existingUser, existingRoles);
                var existingClaims = await _userManager.GetClaimsAsync(existingUser);
                var removeClaimsResult = await _userManager.RemoveClaimsAsync(existingUser, existingClaims);

                var result = await _userManager.DeleteAsync(existingUser);
                if (result.Succeeded)
                {
                    return Ok("Account Denied and Deleted");
                }
                else
                {
                    return Json(new { errors = result.Errors.Select(e => e.Description) });
                }
            }
            return Json(new { errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage) });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] ICRUDModel<UserList> value)
        {
            var user = value.value;

            try
            {
                var existingUser = await _userManager.FindByIdAsync(user.Id);
                if (existingUser != null)
                {
                    existingUser.Email = user.EmailId;
                    existingUser.UserName = user.EmailId;
                    existingUser.Name = user.Name;
                    existingUser.PhoneNumber = user.PhoneNumber;

                    var result = await _userManager.UpdateAsync(existingUser);
                    if (!result.Succeeded)
                    {
                        return Json(new { errors = "Error", message = result.Errors.Select(e => e.Description) });
                    }

                    var existingRoles = await _userManager.GetRolesAsync(existingUser);
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(existingUser, existingRoles);
                    var updateRoleResult = await _userManager.AddToRoleAsync(existingUser, user.Role);

                    return Ok("User edited successfully!");
                }

                return BadRequest("User not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [HttpPost]
        public async Task<IActionResult> RemoveUser([FromBody] ICRUDModel<UserList> value)
        {
            try
            {
                var existingUser = await _userManager.FindByIdAsync(value.key.ToString());
                if (existingUser != null)
                {

                    var existingRoles = await _userManager.GetRolesAsync(existingUser);
                    var removeRolesResult = await _userManager.RemoveFromRolesAsync(existingUser, existingRoles);
                    var existingClaims = await _userManager.GetClaimsAsync(existingUser);
                    var removeClaimsResult = await _userManager.RemoveClaimsAsync(existingUser, existingClaims);

                    var result = await _userManager.DeleteAsync(existingUser);

                    if (result.Succeeded)
                    {
                        return Ok("User edited successfully!");
                    }
                    else
                    {
                        return Json(new { errors = result.Errors.Select(e => e.Description) });
                    }
                }
                return Json(new { errors = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> GetRoles()
        {
            var roles = _roleManager.Roles.ToList();

            var temp = roles.Select(roles => new
            {
                RoleId = roles.Id,
                roleName = roles.Name,
                value = roles.Name
            });

            return Json(new { temp });
        }

        public async Task<IActionResult> AuditLog()
        {
            return View();
        }

        public async Task<IActionResult> Roles()
        {
            return View();
        }

    }
}
