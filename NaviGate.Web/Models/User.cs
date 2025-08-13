using Microsoft.AspNetCore.Identity;

namespace NaviGate.Web.Models
{
    // IdentityUser sınıfı zaten Email, PasswordHash gibi alanları içerir.
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int FirmId { get; set; }
        public Firm Firm { get; set; }
    }
}
