using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NaviGate.Web.Data;
using NaviGate.Web.Models;

namespace NaviGate.Web.Services
{
    public class AiAlertGeneratorService : BackgroundService
    {
        private readonly ILogger<AiAlertGeneratorService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AiAlertGeneratorService(ILogger<AiAlertGeneratorService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AI Uyarı Servisi Başlatıldı.");

            // Servis ilk çalıştığında, önceki döngünün bitmesini beklemek için 5 dakika bekle.
            _logger.LogInformation("Test için 10 saniye bekleniyor...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // TEST
            // await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForPotentialProblems();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Uyarı kontrolü sırasında bir hata oluştu.");
                }

                // Bu taramayı her 4 saatte bir yap.
                _logger.LogInformation("Bir sonraki kontrol için 1 dakika bekleniyor...");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // TEST
                // await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
            }

            _logger.LogInformation("AI Uyarı Servisi Durduruldu.");
        }

        private async Task CheckForPotentialProblems()
        {
            _logger.LogInformation("Potansiyel sorunlar için sevkiyatlar taranıyor...");
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;

                // Tüm aktif sevkiyatları, dökümanları ve hareketleriyle birlikte çekelim.
                // Bu, her kural için veritabanına tekrar tekrar gitmemizi engeller.
                var activeShipments = await dbContext.Shipments
                    .Include(s => s.Documents)
                    .Include(s => s.Trackings)
                    .Where(s => s.Status != ShipmentStatus.Completed && s.Status != ShipmentStatus.Cancelled)
                    .ToListAsync();

                foreach (var shipment in activeShipments)
                {
                    // --- SEVKİYAT BAZLI KURALLAR ---
                    // --- KURAL 1: GECİKME UYARISI (Mevcut Kural) ---
                    if (shipment.EstimatedArrivalUtc < now)
                    {
                        await CreateAlertIfNotExists(dbContext, shipment, "Gecikme Uyarısı",
                            $"'{shipment.ReferenceNumber}' numaralı sevkiyatın {shipment.EstimatedArrivalUtc:dd.MM.yyyy} olan tahmini varış tarihi geçti.", "Uyarı");
                    }

                    // --- KURAL 2: MANTIKSIZ TARİH UYARISI ---
                    if (shipment.EstimatedArrivalUtc < shipment.EstimatedDepartureUtc)
                    {
                        await CreateAlertIfNotExists(dbContext, shipment, "Mantıksız Tarih",
                            $"'{shipment.ReferenceNumber}' numaralı sevkiyatın varış tarihi ({shipment.EstimatedArrivalUtc:dd.MM.yyyy}), kalkış tarihinden ({shipment.EstimatedDepartureUtc:dd.MM.yyyy}) önce olamaz.", "Hata");
                    }

                    // --- KURAL 3: KRİTİK DÖKÜMAN EKSİK UYARILARI (Genişletildi) ---
                    if (shipment.Status == ShipmentStatus.InTransit || shipment.Status == ShipmentStatus.AtCustoms)
                    {
                        // Kontrol edilecek zorunlu döküman tiplerinin bir listesini oluşturalım.
                        var requiredDocTypes = new List<DocumentType>
                        {
                            DocumentType.BillOfLading,
                            DocumentType.Invoice,
                            DocumentType.PackingList,
                            DocumentType.InsurancePolicy
                        };

                        // Sevkiyatta mevcut olan döküman tiplerini bir sete alalım (daha hızlı kontrol için).
                        var existingDocTypes = shipment.Documents.Select(d => d.DocumentType).ToHashSet();

                        // Şimdi her bir zorunlu döküman tipinin sevkiyatta olup olmadığını kontrol edelim.
                        foreach (var requiredType in requiredDocTypes)
                        {
                            if (!existingDocTypes.Contains(requiredType))
                            {
                                // Eğer zorunlu döküman sevkiyatta yoksa, onun için özel bir uyarı oluştur.
                                string missingDocName = GetDocumentTypeName(requiredType); // Dökümanın Türkçe adını alalım.
                                await CreateAlertIfNotExists(dbContext, shipment, "Eksik Döküman",
                                    $"'{shipment.ReferenceNumber}' numaralı sevkiyat yolda ancak '{missingDocName}' sisteme henüz yüklenmemiş.", "Önemli");
                            }
                        }
                    }

                    // --- KURAL 4: GÜMRÜKTE FAZLA BEKLEME UYARISI ---
                    if (shipment.Status == ShipmentStatus.AtCustoms)
                    {
                        var lastTrackingEvent = shipment.Trackings.OrderByDescending(t => t.EventDateUtc).FirstOrDefault();
                        // if (lastTrackingEvent != null && (now - lastTrackingEvent.EventDateUtc) > TimeSpan.FromDays(7))
                        if (lastTrackingEvent != null && (now - lastTrackingEvent.EventDateUtc) > TimeSpan.FromSeconds(30)) // TEST
                        {
                            await CreateAlertIfNotExists(dbContext, shipment, "Gümrükte Fazla Bekleme",
                               $"'{shipment.ReferenceNumber}' numaralı sevkiyat 7 günden uzun süredir gümrükte bekliyor. Son hareket tarihi: {lastTrackingEvent.EventDateUtc:dd.MM.yyyy}", "Kritik");
                        }
                    }

                    // --- KURAL 5: KALKIŞ GECİKMESİ UYARISI ---
                    if ((shipment.Status == ShipmentStatus.Draft || shipment.Status == ShipmentStatus.ReadyForDispatch) && shipment.EstimatedDepartureUtc < now)
                    {
                        await CreateAlertIfNotExists(dbContext, shipment, "Kalkış Gecikmesi",
                            $"'{shipment.ReferenceNumber}' sevkiyatının {shipment.EstimatedDepartureUtc:dd.MM.yyyy} olan tahmini kalkış tarihi geçti ancak durumu hala yola çıkmadı.", "Uyarı");
                    }

                    // --- KURAL 6: HAREKETSİZ SEVKİYAT UYARISI ---
                    if (shipment.Status == ShipmentStatus.InTransit)
                    {
                        var lastTrackingEvent = shipment.Trackings.OrderByDescending(t => t.EventDateUtc).FirstOrDefault();
                        // Eğer hiç hareket yoksa, kalkış tarihini baz al. Varsa son hareketin tarihini al.
                        DateTime lastActivityDate = lastTrackingEvent?.EventDateUtc ?? shipment.EstimatedDepartureUtc;

                        // if ((now - lastActivityDate) > TimeSpan.FromDays(5))
                        if ((now - lastActivityDate) > TimeSpan.FromSeconds(30)) // TEST
                        {
                            await CreateAlertIfNotExists(dbContext, shipment, "Hareketsiz Sevkiyat",
                                $"'{shipment.ReferenceNumber}' numaralı sevkiyattan 5 günden uzun süredir yeni bir hareket bilgisi alınamadı.", "Önemli");
                        }
                    }

                    // --- KURAL 7: EKSİK FİNANSAL BİLGİ UYARISI ---
                    if ((shipment.Status == ShipmentStatus.AtCustoms || shipment.Status == ShipmentStatus.Completed) && (shipment.FreightCost == null || shipment.FreightCost <= 0))
                    {
                        await CreateAlertIfNotExists(dbContext, shipment, "Eksik Finansal Bilgi",
                            $"'{shipment.ReferenceNumber}' sevkiyatı tamamlanma aşamasında ancak navlun ücreti henüz girilmemiş.", "Uyarı");
                    }

                    // --- KURAL 8: AYNI LİMAN UYARISI ---
                    // DeparturePortId ve ArrivalPortId'nin null olmadığını da kontrol edelim.
                    if (shipment.DeparturePortId != 0 && shipment.DeparturePortId == shipment.ArrivalPortId)
                    {
                        await CreateAlertIfNotExists(dbContext, shipment, "Aynı Liman Hatası",
                             $"'{shipment.ReferenceNumber}' sevkiyatının kalkış ve varış limanı aynı olamaz.", "Hata");
                    }

                    // --- DÖKÜMAN BAZLI KURALLAR ---
                    foreach (var doc in shipment.Documents)
                    {
                        // --- YENİ KURAL 9: ONAY SÜRESİ GECİKMİŞ DÖKÜMAN ---
                        // Durumu "Onay Bekliyor" ise ve üzerinden 3 günden fazla geçtiyse
                        if (doc.VerificationStatus == "Onay Bekliyor" && (now - doc.UploadDateUtc) > TimeSpan.FromSeconds(30)) // TEST
                        // if (doc.VerificationStatus == "Onay Bekliyor" && (now - doc.UploadDateUtc) > TimeSpan.FromDays(3))
                        {
                            await CreateAlertIfNotExists(dbContext, shipment, "Gecikmiş Döküman Onayı",
                                $"'{shipment.ReferenceNumber}' sevkiyatına ait '{doc.FileName}' adlı döküman 3 günden uzun süredir onay bekliyor.", "Uyarı");
                        }

                        // --- YENİ KURAL 10: REDDEDİLMİŞ DÖKÜMAN ---
                        // Durumu "Reddedildi" ise, kullanıcıyı bilgilendir.
                        if (doc.VerificationStatus == "Reddedildi")
                        {
                            await CreateAlertIfNotExists(dbContext, shipment, "Reddedilmiş Döküman",
                                $"'{shipment.ReferenceNumber}' sevkiyatı için yüklediğiniz '{doc.FileName}' adlı döküman reddedildi. Lütfen notları kontrol edip yenisini yükleyin.", "Önemli");
                        }
                    }

                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Tarama tamamlandı.");
            }
        }

        // YENİ YARDIMCI METOT: Kod tekrarını önlemek için uyarı oluşturma mantığını merkezileştirir.
        private async Task CreateAlertIfNotExists(ApplicationDbContext context, Shipment shipment, string alertType, string message, string severity)
        {
            bool alertExists = await context.AiAlerts
                .AnyAsync(a => a.ShipmentId == shipment.ShipmentId && a.AlertType == alertType && !a.IsResolved);

            if (!alertExists)
            {
                var newAlert = new AiAlert
                {
                    ShipmentId = shipment.ShipmentId,
                    AlertType = alertType,
                    Description = message,
                    IsResolved = false,
                    Severity = severity,
                    CreatedAtUtc = DateTime.UtcNow
                };
                context.AiAlerts.Add(newAlert);
                _logger.LogWarning($"{alertType} Oluşturuldu: Sevkiyat ID {shipment.ShipmentId}");
            }
        }

        // YENİ EKLENEN YARDIMCI METOT: Enum'un görünen adını alır.
        private string GetDocumentTypeName(DocumentType docType)
        {
            // Bu metot, [Display(Name="...")] etiketini okuyarak "Konşimento", "Fatura" gibi
            // kullanıcı dostu isimleri döndürür. EnumExtensions'a benzer bir mantık kullanır.
            var memberInfo = typeof(DocumentType).GetMember(docType.ToString());
            var displayAttribute = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                                                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;

            return displayAttribute?.Name ?? docType.ToString(); // Eğer DisplayAttribute yoksa, İngilizce adını döndür.
        }
    }
}