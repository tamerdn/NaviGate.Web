#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering; // SelectList için eklendi
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NaviGate.Web.Data; // DbContext için eklendi
using NaviGate.Web.Models;

namespace NaviGate.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context; // Veritabanına erişim için eklendi

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context) // Constructor'a eklendi
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context; // Değişkene atandı
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // Dropdown'ı doldurmak için bu özelliği ekledik
        public SelectList FirmOptions { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "E-posta")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0}, en az {2} en fazla {1} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Şifre")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Şifre Tekrarı")]
            [Compare("Password", ErrorMessage = "Şifre ile şifre tekrarı eşleşmiyor.")]
            public string ConfirmPassword { get; set; }

            // --- YENİ EKLENEN ÖZELLİKLER ---
            [Required(ErrorMessage = "İsim alanı zorunludur.")]
            [Display(Name = "Adınız")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Soyisim alanı zorunludur.")]
            [Display(Name = "Soyadınız")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Firma seçimi zorunludur.")]
            [Display(Name = "Firma")]
            public int FirmId { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Sayfa ilk yüklendiğinde firmalar listesini veritabanından çek ve dropdown için hazırla
            FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive).OrderBy(f => f.FirmName), "FirmId", "FirmName");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Yeni User nesnesini, formdan gelen ek bilgilerle oluşturuyoruz
                var user = new User
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    FirmId = Input.FirmId
                };

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Yeni eklenen kullanıcıya varsayılan olarak "User" rolünü atayalım
                    await _userManager.AddToRoleAsync(user, "User");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code, returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Eğer bir hata varsa ve forma geri dönüyorsak, dropdown'ın boşalmaması için tekrar doldur.
            FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive).OrderBy(f => f.FirmName), "FirmId", "FirmName");
            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
    }
}