using Microsoft.AspNetCore.Mvc.Rendering;

using NaviGate.Web.Models;

using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;



namespace NaviGate.Web.ViewModels

{

    public class ShipmentEditViewModel

    {

        public Shipment Shipment { get; set; }



        // Bu property, JavaScript ile güncellenen konteynerlerin Controller'a gelmesini sağlar.

        public List<Container> Containers { get; set; }

        public List<DocumentEditViewModel> Documents { get; set; }



        public SelectList? FirmOptions { get; set; }

        public SelectList? CarrierOptions { get; set; }

        public SelectList? DeparturePortOptions { get; set; }

        public SelectList? ArrivalPortOptions { get; set; }

        public SelectList? DocumentOptions { get; set; }



        public ShipmentEditViewModel()

        {

            Shipment = new Shipment();

            Containers = new List<Container>();

            Documents = new List<DocumentEditViewModel>();

        }

    }





}