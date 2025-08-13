using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class ContainerIndexViewModel
    {
        public List<Container> Containers { get; set; }

        [Display(Name = "Konteyner Numarası")]
        public string? SearchString { get; set; }

        [Display(Name = "Sevkiyat")]
        public int? ShipmentIdFilter { get; set; }
        public SelectList? Shipments { get; set; }
    }
}
