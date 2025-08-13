using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [Display(Name = "Adı")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyisim alanı zorunludur.")]
        [Display(Name = "Soyadı")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "{0}, en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Tekrarı")]
        [Compare("Password", ErrorMessage = "Şifre ile şifre tekrarı eşleşmiyor.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Firma seçimi zorunludur.")]
        [Display(Name = "Firma")]
        public int FirmId { get; set; }

        [Required(ErrorMessage = "Rol seçimi zorunludur.")]
        [Display(Name = "Rol")]
        public string SelectedRole { get; set; }

        // Dropdown'ları doldurmak için
        public SelectList? FirmOptions { get; set; }
        public SelectList? RoleOptions { get; set; }
    }
}