using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NaviGate.Web.Services
{
    public class MockVirusScanner : IVirusScannerService
    {
        private readonly ILogger<MockVirusScanner> _logger;

        public MockVirusScanner(ILogger<MockVirusScanner> logger)
        {
            _logger = logger;
        }

        public Task<bool> DosyaGuvenliMiAsync(string dosyaYolu)
        {
            var guvenliMi = !Path.GetExtension(dosyaYolu).Equals(".exe", StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(guvenliMi);
        }

        public async Task<VirusScanResult> ScanAsync(string filePath)
        {
            var result = await DosyaGuvenliMiAsync(filePath);
            return new VirusScanResult
            {
                IsThreatDetected = !result,
                ThreatType = result ? null : "ExecutableFile"
            };
        }
    }
}
