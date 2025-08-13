using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class ContainerEditViewModel
    {
        public Container Container { get; set; }

        // Sevkiyat bilgisi sadece görüntüleme amaçlı
        public Shipment? Shipment { get; set; }
    }
}