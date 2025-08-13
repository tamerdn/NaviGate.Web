using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class FirmIndexViewModel
    {
        public List<Firm> Firms { get; set; }

        [Display(Name = "Firma Adı")]
        public string? SearchString { get; set; }

        [Display(Name = "Durum")]
        public bool? IsActiveFilter { get; set; } // true=Aktif, false=Pasif, null=Tümü

        [Display(Name = "Firma Tipi")]
        public string? FirmTypeFilter { get; set; } // Seçilen firma tipini tutar
        public SelectList? FirmTypes { get; set; } // Dropdown'ı dolduracak tiplerin listesi
    }
}
