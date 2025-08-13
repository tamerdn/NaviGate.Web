using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class UserIndexViewModel
    {
        public List<User> Users { get; set; }

        [Display(Name = "Ad veya E-posta")]
        public string? SearchString { get; set; }

        [Display(Name = "Firma")]
        public int? FirmIdFilter { get; set; }
        public SelectList? Firms { get; set; }
    }
}