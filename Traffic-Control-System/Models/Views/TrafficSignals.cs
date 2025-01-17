namespace Traffic_Control_System.Models
{
    public class TrafficSignals
    {
        public int ID { get; set; }
        public int DeviceStreamUID { get; set; }
        public int? NumofViolations { get; set; }
        public string? Address { get; set; }
    }

}