using System.ComponentModel.DataAnnotations; // Data Annotations için bu using gerekli.
using System.ComponentModel;

namespace NaviGate.Web.Models
{
    public class Firm
    {
        [Key]
        public int FirmId { get; set; }

        // --- Temel Bilgiler ---
        [Required(ErrorMessage = "Firma adı alanı boş bırakılamaz.")]
        [StringLength(200, ErrorMessage = "Firma adı 200 karakterden uzun olamaz.")]
        [Display(Name = "Firma Adı")]
        public string FirmName { get; set; }

        [Required(ErrorMessage = "Firma tipi seçmek zorunludur.")]
        [Display(Name = "Firma Tipi")]
        public FirmTypeEnum FirmType { get; set; } // Örn: "Müşteri", "Tedarikçi", "Partner"

        // --- İletişim Bilgileri ---
        [Display(Name = "Telefon Numarası")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta Adresi")]
        public string? Email { get; set; }

        [Display(Name = "İlgili Kişi")]
        public string? ContactPerson { get; set; }

        [Display(Name = "Adres")]
        public string? Address { get; set; }

        [Display(Name = "Web Sitesi")]
        public string? Website { get; set; }

        // --- Resmi Bilgiler ---
        [Display(Name = "Vergi Numarası")]
        public string? TaxNumber { get; set; }

        [Display(Name = "Vergi Dairesi")]
        public string? TaxOffice { get; set; }

        // --- Sistem Bilgileri ---
        [Display(Name = "Aktif mi?")]
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Notlar")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAtUtc { get; set; }

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? ModifiedAtUtc { get; set; }


        // İlişkisel Özellikler (Navigation Properties)
        
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}

