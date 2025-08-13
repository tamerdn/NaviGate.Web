// WindowsDefenderScanner.cs
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace NaviGate.Web.Services
{
    public class WindowsDefenderScanner : IVirusScannerService
    {
        private readonly ILogger<WindowsDefenderScanner> _logger;

        public WindowsDefenderScanner(ILogger<WindowsDefenderScanner> logger)
        {
            _logger = logger;
        }

        public async Task<VirusScanResult> ScanAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Dosya bulunamadı: {filePath}");
                    return new VirusScanResult { IsThreatDetected = true };
                }

                var isSafe = await DosyaGuvenliMiAsync(filePath);
                return new VirusScanResult
                {
                    IsThreatDetected = !isSafe,
                    ThreatType = isSafe ? null : "ExecutableFile"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Virus tarama hatası");
                return new VirusScanResult { IsThreatDetected = true };
            }
        }

        public Task<bool> DosyaGuvenliMiAsync(string dosyaYolu)
        {
            var guvenliMi = !Path.GetExtension(dosyaYolu).Equals(".exe", StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(guvenliMi);
        }
    }
}