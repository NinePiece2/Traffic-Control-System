namespace Traffic_Control_System.Models
{
    public class TrafficViolationsViewModel
    {
        public int ActiveSignalID { get; set; }
    }

    public class TrafficViolation
    {
        public int UID { get; set; }

        public DateTime DateCreated { get; set; }

        public string? LicensePlate { get; set; }
    }

}