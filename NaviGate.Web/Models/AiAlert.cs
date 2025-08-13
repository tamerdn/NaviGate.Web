using System.ComponentModel.DataAnnotations;
namespace NaviGate.Web.Models
{
    // AI ın ürettiği uyarıları saklar. 
    public class AiAlert
    {
        [Key] // Primary Key i [Key] ile ekledik.
        public int AiAlertId { get; set; }
        public int ShipmentId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool IsResolved { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // İlişkisel Özellikler
        public Shipment Shipment { get; set; }
    }
}
