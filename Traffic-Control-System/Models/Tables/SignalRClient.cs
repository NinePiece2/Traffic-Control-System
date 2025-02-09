using System.ComponentModel.DataAnnotations;

namespace Traffic_Control_System.Models
{
    public class SignalRClient
    {
        [Key]
        public int ClientID { get; set; }

        [MaxLength(100)]
        public string ConnectionID { get; set; }

        public int? ActiveSignalID { get; set; }

        [MaxLength(20)]
        public string ClientType { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
