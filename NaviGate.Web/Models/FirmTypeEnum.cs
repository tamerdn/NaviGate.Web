using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.Models
{
    public enum FirmTypeEnum
    {
        [Display(Name = "Müşteri")]
        Musteri,

        [Display(Name = "Tedarikçi")]
        Tedarikci,

        [Display(Name = "Partner")]
        Partner,

        [Display(Name = "Dahili")]
        Dahili
    }
}