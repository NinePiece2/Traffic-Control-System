using System.ComponentModel.DataAnnotations;

namespace Traffic_Control_System.Models
{
    public class UserList
    {
        //[Key]
        public string? Id { get; set; }

        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Email / Username is required.")]
        public string EmailId { get; set; }

        //[Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Phone]
        [Required(ErrorMessage = "Phone Number is required.")]
        public string PhoneNumber { get; set; }
    }
}
