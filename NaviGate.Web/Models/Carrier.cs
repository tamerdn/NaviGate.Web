using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.Models
{
    public class Carrier
    {
        [Key]
        public int CarrierId { get; set; }

        [Required(ErrorMessage = "Taşıyıcı adı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Taşıyıcı Adı")]
        public string CarrierName { get; set; }

        // --- Operasyonel Bilgiler ---
        [StringLength(4, ErrorMessage = "SCAC Kodu 4 karakter olmalıdır.")]
        [Display(Name = "SCAC Kodu")]
        public string? ScacCode { get; set; } // Standard Carrier Alpha Code (Deniz/Kara yolu için)

        [Url(ErrorMessage = "Lütfen geçerli bir web adresi girin.")]
        [Display(Name = "Web Sitesi")]
        public string? Website { get; set; }

        [Url(ErrorMessage = "Lütfen geçerli bir takip adresi girin.")]
        [Display(Name = "Takip Adresi (URL)")]
        public string? TrackingUrl { get; set; }

        // --- İletişim Bilgileri ---
        [Display(Name = "İlgili Kişi")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [Display(Name = "E-posta Adresi")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Telefon Numarası")]
        public string? PhoneNumber { get; set; }

        // --- Sistem Bilgileri ---
        [Display(Name = "Aktif mi?")]
        [DefaultValue(true)]
        public bool IsActive { get; set; } = true;

        [DataType(DataType.MultilineText)]
        [Display(Name = "Notlar")]
        public string? Notes { get; set; }
    }
}
