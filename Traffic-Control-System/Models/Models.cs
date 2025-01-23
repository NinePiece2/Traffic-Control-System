using System.ComponentModel.DataAnnotations;

namespace Traffic_Control_System.Models
{
    public class  TrafficViolationsModel
    {
        [Required]
        public int ActiveSignalID { get; set; }

        [Required(ErrorMessage = "License Plate is required.")]
        [StringLength(10, ErrorMessage = "License Plate cannot exceed 10 characters.")]
        public string? LicensePlate { get; set; }

        [Required(ErrorMessage = "Video URL is required.")]
        public string? VideoURL { get; set; }
    }
    
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
        public string? PhoneNumber { get; set; }
    }

    public class ApproveDenyModel
    {
        public string ID { get; set; }
        public bool textArea { get; set; }
        public string confirmBtnMessage { get; set; }
        public string hintMessage { get; set; }
        public string cancelBtnMessage { get; set; }
        public string reminderText { get; set; }
    }

    public class ICRUDModel<T> where T : class
    {
        public string action { get; set; }

        public string table { get; set; }

        public string keyColumn { get; set; }

        public object key { get; set; }

        public T value { get; set; }

        public List<T> added { get; set; }

        public List<T> changed { get; set; }

        public List<T> deleted { get; set; }

        public IDictionary<string, object> @params { get; set; }
    }

    public class VideStreamViewModel
    {
        public string VideoURL { get; set; } = "~/VideoServiceProxy/";
    }

    public class ReportViewModel
    {
        public DateTime DateCreated { get; set; }
        public string? LicensePlate { get; set; }
        public string VideoURL { get; set; } = "~/VideoServiceProxy/";
    }
}
