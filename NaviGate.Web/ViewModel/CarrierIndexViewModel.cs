using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class CarrierIndexViewModel
    {
        public List<Carrier> Carriers { get; set; }

        [Display(Name = "Taşıyıcı Adı")]
        public string? SearchString { get; set; }

        [Display(Name = "Durum")]
        public bool? IsActiveFilter { get; set; }
    }
}
