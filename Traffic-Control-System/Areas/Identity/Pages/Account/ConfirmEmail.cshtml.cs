// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Traffic_Control_System.Data;
using Traffic_Control_System.Models;
using Traffic_Control_System.Services;

namespace Traffic_Control_System.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";


            var adminEmailsList = _context.Users.Where(u => u.EmailConfirmed && 
                                _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                                                       _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
                            .Select(u => u.Email)
                            .ToList();

            var adminEmails = String.Join(";", adminEmailsList);
            await _emailService.SendEmail(adminEmails, "New Account Awaiting Approval", false,GetEmailHTML(user.Name, user.Email), true, null, null);
            return Page();
        }

        public string GetEmailHTML(string name, string email)
        {
            var baseUrl = _configuration["BaseUrl"];
            var htmlEmailBody = $@"<!DOCTYPE html>
                                        <html lang='en'>
                                        <head>
                                            <meta charset='UTF-8'>
                                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                            <title>Traffic Control System - New Account Awaiting Approval</title>
                                            <style>
                                                body {{
                                                    font-family: Arial, sans-serif;
                                                    line-height: 1.6;
                                                    background-color: #f4f4f4;
                                                    margin: 0;
                                                    padding: 0;
                                                }}
                                                .container {{
                                                    max-width: 600px;
                                                    margin: 20px auto;
                                                    background-color: #fff;
                                                    padding: 20px;
                                                    border-radius: 5px;
                                                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                                                }}
                                                table {{
                                                    width: 100%;
                                                    border-collapse: collapse;
                                                    margin-top: 20px;
                                                }}
                                                th, td {{
                                                    padding: 10px;
                                                    text-align: left;
                                                    border-bottom: 1px solid #ddd;
                                                }}
                                                th {{
                                                    background-color: #f2f2f2;
                                                }}
                                                h2 {{
                                                    color: #333;
                                                }}
                                            </style>
                                        </head>
                                        <body>
                                            <div class='container'>
                                                <h2>Traffic Control System - New Account Awaiting Approval</h2>
                                                <table>
                                                    <tr>
                                                        <th>Name</th>
                                                        <td>{Capitalize(name)}</td>
                                                    </tr>
                                                    <tr>
                                                        <th>Email</th>
                                                        <td>{email}</td>
                                                    </tr>
                                                   
                                                </table>
                                                <p>
                                                    Please review and approve this account as soon as possible.
                                                </p>
                                                <p>
                                                    <a href=""{baseUrl}"">Login to Traffic Control System</a>
                                                </p>
                                            </div>
                                        </body>
                                        </html>";
            return htmlEmailBody;
        }

        public static string Capitalize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Split the string into words
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Capitalize each word
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (word.Length > 0)
                {
                    words[i] = char.ToUpper(word[0]) + word.Substring(1).ToLower();
                }
            }

            // Join the words back into a single string
            return string.Join(" ", words);
        }
    }
}
