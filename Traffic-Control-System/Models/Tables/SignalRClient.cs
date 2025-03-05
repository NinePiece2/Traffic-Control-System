namespace Traffic_Control_System.Models
{
    public class SignalRClient
    {
        public int UID { get; set; }
        public string ConnectionID { get; set; }
        public int? ActiveSignalID { get; set; }
        public string ClientType { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}