using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NaviGate.Web.ViewModels
{
    public class DocumentCreateViewModel
    {
        [Display(Name = "Sevkiyat")]
        public int ShipmentId { get; set; }

        [Required]
        [Display(Name = "Döküman Tipi")]
        public DocumentType DocumentType { get; set; }

        [Required]
        [Display(Name = "Dosya")]
        public IFormFile DocumentFile { get; set; }

        // Dropdown için
        public SelectList? ShipmentOptions { get; set; }
    }
}