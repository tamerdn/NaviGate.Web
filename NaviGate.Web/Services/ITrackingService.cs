using NaviGate.Web.Models;
using System.Threading.Tasks;

namespace NaviGate.Web.Services
{
    public interface ITrackingService
    {
        Task<ShipmentTracking> GetLatestTrackingUpdateAsync(Shipment shipment);
    }
}