using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.Models
{
    public enum DocumentType { 
        [Display(Name ="Fatura")]
        Invoice, 
        [Display(Name = "Konşimento")]
        BillOfLading, 
        [Display(Name = "ÇekiListesi")]
        PackingList,
        [Display(Name = "SigortaPoliçesi")]
        InsurancePolicy, 
        [Display(Name = "Diğer")]
        Other
        }

public class Document
    {
        public int DocumentId { get; set; }

        [Display(Name = "Ait Olduğu Sevkiyat")]
        public int ShipmentId { get; set; }

        [Required]
        [Display(Name = "Döküman Tipi")]
        public DocumentType DocumentType { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Dosya Adı")]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Dosya Yolu")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "Dosya Boyutu")]
        public long? FileSizeInBytes { get; set; }

        [Display(Name = "Dosya Türü(jpg,pdf,jpeg..)")]
        public string? MimeType { get; set; }

        [Display(Name = "Yüklenme Tarihi")]
        public DateTime UploadDateUtc { get; set; } = DateTime.UtcNow;

        [Display(Name = "Son Güncellenme Tarihi")]
        public DateTime? ModifiedAtUtc { get; set; }

        [Required]
        [Display(Name = "Yükleyen Kullanıcı")]
        public string UploadedByUserId { get; set; } = string.Empty;

        [Display(Name = "Doğrulama Durumu")]
        public string VerificationStatus { get; set; } = "Pending";

        [Display(Name = "Doğrulama Notları")]
        public string? VerificationNotes { get; set; }

        // İlişkisel Özellikler
        [Display(Name = "Ait Olduğu Sevkiyat")]
        public Shipment Shipment { get; set; }

        [Display(Name = "Yükleyen Kullanıcı")]
        public User UploadedByUser { get; set; }

        [Required]
        [StringLength(36)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    }
}