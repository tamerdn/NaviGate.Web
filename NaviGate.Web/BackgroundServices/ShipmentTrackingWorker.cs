using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NaviGate.Web.Data;
using NaviGate.Web.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NaviGate.Web.BackgroundServices
{
    public class ShipmentTrackingWorker : BackgroundService
    {
        private readonly ILogger<ShipmentTrackingWorker> _logger;
        private readonly IServiceProvider _services;

        public ShipmentTrackingWorker(ILogger<ShipmentTrackingWorker> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Sevkiyat Takip Hizmeti çalışıyor: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var trackingService = scope.ServiceProvider.GetRequiredService<ITrackingService>();

                    var activeShipments = await context.Shipments
                        .Where(s => s.Status == Models.ShipmentStatus.InTransit)
                        .ToListAsync(stoppingToken);

                    foreach (var shipment in activeShipments)
                    {
                        var latestUpdate = await trackingService.GetLatestTrackingUpdateAsync(shipment);

                        // Aynı güncelleme zaten var mı diye kontrol et
                        var lastTracking = await context.ShipmentTrackings
                            .Where(t => t.ShipmentId == shipment.ShipmentId)
                            .OrderByDescending(t => t.EventDateUtc)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (lastTracking == null || lastTracking.StatusDescription != latestUpdate.StatusDescription)
                        {
                            context.ShipmentTrackings.Add(latestUpdate);
                            _logger.LogInformation($"Sevkiyat {shipment.ReferenceNumber} için yeni hareket eklendi: {latestUpdate.StatusDescription}");
                        }
                    }
                    await context.SaveChangesAsync(stoppingToken);
                }

                // Her 1 saatte bir çalışacak şekilde ayarla
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
