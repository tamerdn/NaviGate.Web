using System.Threading.Tasks;

namespace NaviGate.Web.Services
{
    // Tarama sonucunu temsil eden sınıf
    public class VirusScanResult
    {
        public bool IsThreatDetected { get; set; }
        public string? ThreatType { get; set; }
    }

    // Virüs tarama servislerinin uyması gereken sözleşme (arayüz)
    public interface IVirusScannerService
    {
        Task<VirusScanResult> ScanAsync(string filePath);
    }
}