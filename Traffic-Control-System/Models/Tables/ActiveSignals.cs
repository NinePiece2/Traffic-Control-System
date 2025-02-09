namespace Traffic_Control_System.Models
{
    public class ActiveSignals
    {
        public int ID { get; set; }
        public string Address { get; set; }  // Made non-nullable since it's required
        public string? Direction1 { get; set; } // New field for traffic signal direction
        public string? Direction2 { get; set; } // New field for traffic signal direction
        public int? Direction1Green { get; set; } // Green signal for traffic signal direction
        public int? Direction2Green { get; set; } // Green signal for traffic signal direction
        public int? DeviceStreamUID { get; set; } // Changed to string to match API generation
        public bool? IsActive { get; set; }
    }
}