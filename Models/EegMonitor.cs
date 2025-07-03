// Models/EegData.cs - Simple EEG data structure
namespace EegMonitor.Models
{
    public class EegData
    {
        // Raw signal values (simulated EEG channels)
        public List<double> RawValues { get; set; } = new List<double>();

        // Frequency bands (calculated from raw values)
        public double Delta { get; set; }  // 1-4 Hz
        public double Theta { get; set; }  // 4-8 Hz
        public double Alpha { get; set; }  // 8-13 Hz
        public double Beta { get; set; }   // 13-30 Hz
        public double Gamma { get; set; }  // 30+ Hz

        //public DateTime Timestamp { get; set; }
    }
}