using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.Models
{
    public class ShipmentTracking
    {
        public int ShipmentTrackingId { get; set; }

        [Display(Name = "Sevkiyat")]
        public int ShipmentId { get; set; }
            
        [Display(Name = "Lokasyon")]
        public string? Location { get; set; }

        [Required]
        [Display(Name = "Durum Açıklaması")]
        public string StatusDescription { get; set; }

        [Display(Name = "Olay Tarihi")]
        public DateTime EventDateUtc { get; set; }

        // İlişkisel Özellikler
        public Shipment Shipment { get; set; }
    }
}