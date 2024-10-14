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
}
