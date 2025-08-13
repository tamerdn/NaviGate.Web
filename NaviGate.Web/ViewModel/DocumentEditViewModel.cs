using Microsoft.AspNetCore.Http;
using NaviGate.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class DocumentEditViewModel
    {
        public int DocumentId { get; set; } // Id yerine DocumentId olmalı

        [Display(Name = "Ait Olduğu Sevkiyat")]
        public int ShipmentId { get; set; }

        [Required(ErrorMessage = "Lütfen döküman tipini seçin.")]
        [Display(Name = "Döküman Tipi")]
        public DocumentType DocumentType { get; set; }

        [Display(Name = "Doğrulama Durumu")]
        public string? VerificationStatus { get; set; } // Enum yerine string yapın

        [Display(Name = "Doğrulama Notları")]
        public string? VerificationNotes { get; set; }

        [Display(Name = "Mevcut Dosya Adı")]
        public string? FileName { get; set; } // ExistingFileName yerine FileName

        [Display(Name = "Yeni Dosya (İsteğe Bağlı)")]
        public IFormFile? NewDocumentFile { get; set; }

        public string? UploadedByUserId { get; set; }
        public string? FilePath { get; set; }
        public IFormFile? FormFile { get; set; }
    }
}
