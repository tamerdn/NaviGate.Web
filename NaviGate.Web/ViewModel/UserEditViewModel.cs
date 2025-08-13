using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NaviGate.Web.ViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Adı")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Soyadı")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Firma")]
        public int FirmId { get; set; }

        [Required]
        [Display(Name = "Rol")]
        public string SelectedRole { get; set; }

        public string FirmName { get; set; } 


        // Dropdown'lar için
        public SelectList? FirmOptions { get; set; }
        public SelectList? RoleOptions { get; set; }
    }
}