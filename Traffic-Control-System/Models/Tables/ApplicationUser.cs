using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Traffic_Control_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(255)]
        public string? Name { get; set; }
        public bool AccountApproved { get; set; }
    }
}
