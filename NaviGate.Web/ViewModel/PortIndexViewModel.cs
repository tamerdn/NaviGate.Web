using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class PortIndexViewModel
    {
        public List<Port> Ports { get; set; }

        [Display(Name = "Liman Adı / Kodu")]
        public string? SearchString { get; set; }

        [Display(Name = "Ülke")]
        public string? CountryFilter { get; set; }

        // Ülke filtresi için dropdown listesini tutacak
        public SelectList? Countries { get; set; }
    }
}
