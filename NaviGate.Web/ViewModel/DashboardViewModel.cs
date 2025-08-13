using NaviGate.Web.Models;
using System.Collections.Generic;

namespace NaviGate.Web.ViewModels
{
    public class DashboardViewModel
    {
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string UserRole { get; set; }
        public int UnresolvedAlertsCount { get; set; }
        public int ActiveShipmentsCount { get; set; }
        public int PendingDocumentsCount { get; set; }
        public int CompletedShipmentsLast30DaysCount { get; set; }
        public List<ShipmentTracking> RecentTrackings { get; set; }
        public List<Shipment> RecentShipments { get; set; }

        public DashboardViewModel()
        {
            RecentTrackings = new List<ShipmentTracking>();
            RecentShipments = new List<Shipment>();
            UserFirstName = string.Empty;
            UserLastName = string.Empty;
            UserRole = string.Empty;
        }
    }
}
