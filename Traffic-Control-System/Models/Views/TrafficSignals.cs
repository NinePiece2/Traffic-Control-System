namespace Traffic_Control_System.Models
{
    public class TrafficSignals
    {
        public int ID { get; set; }
        public string Address { get; set; }
        public string? Direction1 { get; set; }
        public string? Direction2 { get; set; }
        public int? Direction1Green { get; set; }
        public int? Direction2Green { get; set; }
        public int? PedestrianWalkTime { get; set; }
        public int? DeviceStreamUID { get; set; }
        public int? NumofViolations { get; set; }
        public DateTime? LatestViolationDate { get; set; }
    }
}