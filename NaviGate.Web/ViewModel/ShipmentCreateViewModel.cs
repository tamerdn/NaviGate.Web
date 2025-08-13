using Microsoft.AspNetCore.Mvc.Rendering;
using NaviGate.Web.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class ShipmentCreateViewModel
    {
        public Shipment Shipment { get; set; }

        // Bu property, JavaScript ile eklenen konteynerlerin Controller'a gelmesini sağlar.
        public List<Container> Containers { get; set; }
        public List<DocumentCreateViewModel> Documents { get; set; }

        // Dropdown'ları Doldurmak İçin Gerekli Listeler
        public SelectList? FirmOptions { get; set; }
        public SelectList? CarrierOptions { get; set; }
        public SelectList? PortOptions { get; set; }
        public SelectList? DocumentOptions { get; set; }

        public ShipmentCreateViewModel()
        {
            Shipment = new Shipment();
            Containers = new List<Container>();
            Documents = new List<DocumentCreateViewModel>();
        }
    }
    

}