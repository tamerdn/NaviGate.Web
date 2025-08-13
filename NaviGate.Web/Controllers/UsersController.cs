using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.ViewModels;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NaviGate.Web.Controllers
{
    // Bu sayfaya sadece Admin ve Manager'lar erişebilir.
    [Authorize(Roles = "Admin,Manager,User")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Users
        public async Task<IActionResult> Index(string searchString, int? firmIdFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<User> usersQuery;

            // YENİ ROL BAZLI FİLTRELEME
            if (User.IsInRole("Admin"))
            {
                usersQuery = _userManager.Users.Include(u => u.Firm);
            }
            else if (User.IsInRole("Manager"))
            {
                // Manager kendi firmasındaki tüm kullanıcıları görür.
                usersQuery = _userManager.Users.Where(u => u.FirmId == currentUser.FirmId).Include(u => u.Firm);
            }
            else // User
            {
                // User sadece kendini görür.
                usersQuery = _userManager.Users.Where(u => u.Id == currentUser.Id).Include(u => u.Firm);
            }

            // Temel sorgu (UserManager'dan tüm kullanıcıları al)
            //  IQueryable<User> usersQuery = _userManager.Users.Include(u => u.Firm);

            // ARAMA: Ad veya e-postaya göre
            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.FirstName.Contains(searchString) || u.LastName.Contains(searchString) || u.Email.Contains(searchString));
            }

            // FİLTRELEME: Firmaya göre
            if (firmIdFilter.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.FirmId == firmIdFilter.Value);
            }

            var viewModel = new UserIndexViewModel
            {
                Firms = new SelectList(await _context.Firms.OrderBy(f => f.FirmName).ToListAsync(), "FirmId", "FirmName", firmIdFilter),
                Users = await usersQuery.OrderBy(u => u.FirstName.Contains(searchString) || u.LastName.Contains(searchString)).ToListAsync(),
                SearchString = searchString,
                FirmIdFilter = firmIdFilter
            };

            return View(viewModel);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new UserCreateViewModel();
            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);

                var user = new User
                {
                    UserName = viewModel.Email,
                    Email = viewModel.Email,
                    FirstName = viewModel.FirstName,
                    LastName = viewModel.LastName,
                    EmailConfirmed = true // Yeni kullanıcıları direkt onaylı yapalım
                };

                // Güvenlik: Manager, sadece kendi firmasına kullanıcı ekleyebilir.
                if (User.IsInRole("Manager"))
                {
                    user.FirmId = currentUser.FirmId;
                }
                else // Admin ise formdan seçileni kullanır
                {
                    user.FirmId = viewModel.FirmId;
                }

                var result = await _userManager.CreateAsync(user, viewModel.Password);
                if (result.Succeeded)
                {
                    // Güvenlik: Manager, sadece "User" rolü atayabilir.
                    if (User.IsInRole("Manager") && viewModel.SelectedRole != "User")
                    {
                        ModelState.AddModelError("", "Manager yetkisiyle sadece 'User' rolü atanabilir.");
                        await PopulateDropdowns(viewModel);
                        return View(viewModel);
                    }

                    await _userManager.AddToRoleAsync(user, viewModel.SelectedRole);
                    TempData["SuccessMessage"] = $"'{user.FirstName}' adlı kullanıcı başarıyla oluşturuldu.";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Hata durumunda dropdown'ları tekrar doldur
            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(string id) // User ID'si string (GUID) olduğu için parametre string
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Firm) // Kullanıcının firmasını da getirelim
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Kullanıcının rollerini bulalım
            var userRoles = await _userManager.GetRolesAsync(user);
            ViewData["Roles"] = userRoles; // Rolleri View'a gönderelim

            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            var viewModel = new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FirmId = user.FirmId,
                SelectedRole = userRoles.FirstOrDefault()
            };

            viewModel.FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive), "FirmId", "FirmName", user.FirmId);
            viewModel.RoleOptions = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", userRoles.FirstOrDefault());

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound();

                // Kullanıcı bilgilerini güncelle
                user.FirstName = viewModel.FirstName;
                user.LastName = viewModel.LastName;
                user.Email = viewModel.Email;
                user.UserName = viewModel.Email;

                // Firma ve rol yetki kontrolleri
                var currentUser = await _userManager.GetUserAsync(User);

                if (User.IsInRole("Admin"))
                {
                    user.FirmId = viewModel.FirmId;
                }
                else if (user.FirmId != currentUser.FirmId)
                {
                    // Manager başka firmadaki kullanıcıyı düzenleyemez
                    return Forbid();
                }

                // Rol güncelleme
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (viewModel.SelectedRole != currentRoles.FirstOrDefault())
                {
                    // Rol değişikliği yetki kontrolü
                    if (User.IsInRole("Admin") ||
                        (User.IsInRole("Manager") && (viewModel.SelectedRole == "Manager" || viewModel.SelectedRole == "User")))
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, viewModel.SelectedRole);
                    }
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    TempData["SuccessMessage"] = $"'{user.FirstName}' adlı kullanıcı başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Hata durumunda dropdown'ları tekrar doldur
            viewModel.FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive), "FirmId", "FirmName", viewModel.FirmId);
            viewModel.RoleOptions = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", viewModel.SelectedRole);
            return View(viewModel);
        }

        // GET: Users/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    // Hata yönetimi
                    return View("Error");
                }
            }
            TempData["SuccessMessage"] = $"'{user.FirstName}' adlı kullanıcı başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        // --- YARDIMCI METOT ---
        private async Task PopulateDropdowns(UserCreateViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
            {
                viewModel.FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive), "FirmId", "FirmName");
                // Admin, Manager veya User atayabilir
                viewModel.RoleOptions = new SelectList(await _roleManager.Roles.Where(r => r.Name != "Admin").ToListAsync(), "Name", "Name");
            }
            else // Manager ise
            {
                // Manager sadece kendi firmasını seçebilir (veya göstermeye gerek bile yok)
                viewModel.FirmOptions = new SelectList(_context.Firms.Where(f => f.FirmId == currentUser.FirmId), "FirmId", "FirmName");
                // Manager sadece User atayabilir
                viewModel.RoleOptions = new SelectList(await _roleManager.Roles.Where(r => r.Name == "User").ToListAsync(), "Name", "Name");
            }
        }
    }
}
    
