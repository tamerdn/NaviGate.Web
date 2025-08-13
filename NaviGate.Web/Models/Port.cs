// Port.cs
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.Models
{
    public class Port
    {
        public int PortId { get; set; }
        [Required]
        [StringLength(150)]
        [Display(Name = "Liman Adı")]
        public string PortName { get; set; }

        [Required]
        [StringLength(5)]
        [Display(Name = "Liman Kodu")]
        public string PortCode { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Ülke")]
        public string Country { get; set; }
    }
}