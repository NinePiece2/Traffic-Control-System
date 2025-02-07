namespace Traffic_Control_System.Models
{
    public class TrafficSignals
    {
        public int ID { get; set; }
        public string Address { get; set; }  // Made non-nullable since it's required
        public string Direction { get; set; } // New field for traffic signal direction
        public string DeviceStreamId { get; set; } // Changed to string to match API generation
        public string ApiKey { get; set; } // New API Key field
        public int? NumofViolations { get; set; } // Kept this as it was
    }
}