using System.Threading.Tasks;

namespace NaviGate.Web.Services
{
    public class FakeVirusScannerService : IVirusScannerService
    {
        // Sözleşmeye uygun olarak 'VirusScanResult' döndürüyor
        public Task<VirusScanResult> ScanAsync(string filePath)
        {
            // Bu sahte servis, şimdilik her zaman "tehdit yok" diyecek.
            var result = new VirusScanResult
            {
                IsThreatDetected = false,
                ThreatType = "No threats found."
            };
            return Task.FromResult(result);
        }
    }
}