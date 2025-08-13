using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NaviGate.Web.Models
{
    public enum ShipmentStatus
    {
        [Display(Name = "Taslak")]
        Draft=0,
        [Display(Name = "Gönderime Hazır")]
        ReadyForDispatch=1,
        [Display(Name = "Yolda")]
        InTransit=2,
        [Display(Name = "Gümrükte")]
        AtCustoms=3,
        [Display(Name = "Tamamlandı")]
        Completed=4,
        [Display(Name = "İptal Edildi")]
        Cancelled=5
    }

    [Index(nameof(ReferenceNumber), IsUnique = true)]
    public class Shipment
    {
        [Key]
        public int ShipmentId { get; set; }

        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        [Display(Name = "Firma")]
        public int FirmId { get; set; }

        [Required(ErrorMessage = "Referans numarası zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Referans No")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Durum")]
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;

        [Required(ErrorMessage = "Taşıyıcı seçimi zorunludur.")]
        [Display(Name = "Taşıyıcı Firma")]
        public int CarrierId { get; set; }

        [Required(ErrorMessage = "Kalkış limanı seçimi zorunludur.")]
        [Display(Name = "Kalkış Limanı")]
        public int DeparturePortId { get; set; }

        [Required(ErrorMessage = "Varış limanı seçimi zorunludur.")]
        [Display(Name = "Varış Limanı")]
        public int ArrivalPortId { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Tahmini Kalkış Tarihi")]
        public DateTime EstimatedDepartureUtc { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Tahmini Varış Tarihi")]
        public DateTime EstimatedArrivalUtc { get; set; }
        /*
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Navlun Ücreti")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = false)] // ApplyFormatInEditMode=false olmalı
        public decimal? FreightCost { get; set; }*/

        [Display(Name = "Navlun Ücreti")]
        // YENİ EKLENEN KURALLAR:
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? FreightCost { get; set; }

        [StringLength(100)]
        [Display(Name = "Teslim Şekli")]
        public string? Incoterms { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAtUtc { get; set; }

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? ModifiedAtUtc { get; set; }

        [Display(Name = "Oluşturan Kullanıcı")]
        public string CreatedByUserId { get; set; } = string.Empty;

        // --- İlişkisel Özellikler ---
        [Display(Name = "Firma")]
        public virtual Firm? Firm { get; set; }

        [Display(Name = "Oluşturan Kullanıcı")]
        public virtual User? CreatedByUser { get; set; }

        [Display(Name = "Taşıyıcı Firma")]
        public virtual Carrier? Carrier { get; set; }

        [ForeignKey("DeparturePortId")]
        [Display(Name = "Kalkış Limanı")]
        public virtual Port? DeparturePort { get; set; }

        [ForeignKey("ArrivalPortId")]
        [Display(Name = "Varış Limanı")]
        public virtual Port? ArrivalPort { get; set; }

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<AiAlert> AiAlerts { get; set; } = new List<AiAlert>();
        public virtual ICollection<ShipmentTracking> Trackings { get; set; } = new List<ShipmentTracking>();
        public virtual ICollection<Container> Containers { get; set; } = new List<Container>();
    }
}