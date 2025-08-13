using Microsoft.AspNetCore.Http;
using NaviGate.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class DocumentViewModel
    {
        // Bu property, bir dökümanın yeni mi yoksa mevcut mu olduğunu anlamamızı sağlar.
        // Yeni bir döküman için değeri 0, mevcut bir döküman için ise kendi ID'si olacaktır.
        public int DocumentId { get; set; }

        public int ShipmentId { get; set; }

        [Required(ErrorMessage = "Lütfen döküman tipini seçin.")]
        [Display(Name = "Döküman Tipi")]
        public DocumentType DocumentType { get; set; }

        // Hem YENİ döküman yüklerken hem de MEVCUT bir dökümanı değiştirirken
        // yüklenecek dosyayı bu tek property tutacaktır.
        [Display(Name = "Dosya")]
        public IFormFile? File { get; set; }

        // Sadece Edit sayfasında, mevcut dosyanın adını göstermek için kullanılır.
        // Bu property'e formdan bir veri gelmez.
        public string? ExistingFileName { get; set; }
        public string? VerificationStatus { get; set; }
    }
}