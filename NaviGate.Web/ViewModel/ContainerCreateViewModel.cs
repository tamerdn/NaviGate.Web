using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class ContainerCreateViewModel
    {
        public Container Container { get; set; }

        [Required(ErrorMessage = "Lütfen bir sevkiyat seçin.")]
        [Display(Name = "Ait Olduğu Sevkiyat")]
        public int ShipmentId { get; set; }

        // Sadece yetkili olunan sevkiyatları listeleyecek
        public SelectList? AuthorizedShipments { get; set; }
    }
}
