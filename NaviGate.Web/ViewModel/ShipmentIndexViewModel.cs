using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.Models.ViewModels
{
    public class ShipmentIndexViewModel
    {
        // Filtrelenmiş sonuçların gösterileceği liste
        public IEnumerable<Shipment> Shipments { get; set; }

        // Filtreleme dropdown'larını doldurmak için kullanılacak listeler
        public SelectList? Carriers { get; set; }
        public SelectList? Statuses { get; set; }

        // Formdan gelen ve forma geri gönderilecek filtre değerleri
        [Display(Name = "Taşıyıcı Firma")]
        public int? CarrierIdFilter { get; set; }

        [Display(Name = "Durum")]
        public ShipmentStatus? StatusFilter { get; set; }

        [Display(Name = "Referans No")]
        public string? SearchString { get; set; }
        public bool IsOwner { get; set; }

    }
}