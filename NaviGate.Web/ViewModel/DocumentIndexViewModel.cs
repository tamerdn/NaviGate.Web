using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class DocumentIndexViewModel
    {
        public List<Document> Documents { get; set; }

        [Display(Name = "Dosya Adı")]
        public string? SearchString { get; set; }

        [Display(Name = "Sevkiyat")]
        public int? ShipmentIdFilter { get; set; }

        [Display(Name = "Döküman Tipi")]
        public DocumentType? DocumentTypeFilter { get; set; }

        public SelectList? Shipments { get; set; }
        public SelectList? DocTypes { get; set; }
    }
}