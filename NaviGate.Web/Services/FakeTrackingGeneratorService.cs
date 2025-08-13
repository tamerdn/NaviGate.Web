using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace NaviGate.Web.Services
{
    public class FakeTrackingGeneratorService : BackgroundService
    {
        private readonly ILogger<FakeTrackingGeneratorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Random _random = new Random();

        // Sahte veri üretmek için kullanacağımız listeler
        private readonly string[] _fakeLocations = { "Singapur Limanı", "Süveyş Kanalı", "Cebelitarık Boğazı", "Rotterdam Limanı", "Atlas Okyanusu", "Hamburg Gümrüğü" };
        private readonly string[] _fakeStatusDescriptions = { "Gemi seyrine devam ediyor.", "Planlanan rotada ilerliyor.", "Hava koşulları nedeniyle hafif gecikmeli.", "Liman sırası bekleniyor.", "Konteynerler yeniden düzenleniyor." };

        public FakeTrackingGeneratorService(ILogger<FakeTrackingGeneratorService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sahte Sevkiyat Hareketi Üretme Servisi Başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateFakeTrackingData();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sahte veri üretimi sırasında bir hata oluştu.");
                }

                // Her 1 dakikada bir çalışacak şekilde ayarlandı.
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Sahte Sevkiyat Hareketi Üretme Servisi Durduruldu.");
        }

        private async Task GenerateFakeTrackingData()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // --- DEĞİŞİKLİK BURADA ---
                // Enum yerine metin (string) bir değerle karşılaştırma yapıyoruz.
                //const string statusToTrack = "Yolda";

                var shipmentsInTransit = await dbContext.Shipments
                    // DİKKAT: 's.Status' kısmındaki 'Status' property adının 
                    // sizin Shipment modelinizdeki isimle aynı olduğundan emin olun.
                    .Where(s => s.Status == ShipmentStatus.InTransit)
                    .ToListAsync();

                if (!shipmentsInTransit.Any())
                {
                    _logger.LogInformation("Durumu 'Yolda' olan aktif sevkiyat bulunamadı.");
                    return;
                }

                _logger.LogInformation($"{shipmentsInTransit.Count} adet 'Yolda' durumunda sevkiyat bulundu. Hareketler üretiliyor...");

                foreach (var shipment in shipmentsInTransit)
                {
                    var newTracking = new ShipmentTracking
                    {
                        ShipmentId = shipment.ShipmentId,
                        Location = _fakeLocations[_random.Next(_fakeLocations.Length)],
                        StatusDescription = _fakeStatusDescriptions[_random.Next(_fakeStatusDescriptions.Length)],
                        EventDateUtc = DateTime.UtcNow
                    };

                    dbContext.ShipmentTrackings.Add(newTracking);
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"{shipmentsInTransit.Count} sevkiyat için yeni hareketler başarıyla kaydedildi.");
            }
        }
    }
}
