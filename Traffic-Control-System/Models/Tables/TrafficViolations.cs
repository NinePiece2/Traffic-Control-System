namespace Traffic_Control_System.Models
{
    public class TrafficViolations
    {
        public int UID { get; set; }
        public int ActiveSignalID { get; set; }
        public DateTime DateCreated { get; set; }
        public string? LicensePlate { get; set; }
        public string? Filename { get; set; }
    }
}
