using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NaviGate.Web.Models
{
    public class Container
    {
        [Key]
        public int ContainerId { get; set; }

        [Required]
        [Display(Name = "Sevkiyat")]
        public int ShipmentId { get; set; }

        [Required(ErrorMessage = "Konteyner numarası zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Konteyner Numarası")]
        public string ContainerNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konteyner tipi zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Konteyner Tipi")]
        public string ContainerType { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Mühür Numarası")]
        public string? SealNumber { get; set; }

        // --- DEĞİŞİKLİK BURADA ---
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Brüt Ağırlık (kg)")]
        public decimal? GrossWeightKg { get; set; } // Nullable yapıldı

        [Display(Name = "Paket Adedi")]
        public int? PackageQuantity { get; set; } // Nullable yapıldı
                                                  // -------------------------
        [Display(Name = "Sevkiyat Referans Numarası")]
        public virtual Shipment? Shipment { get; set; }
    }
}