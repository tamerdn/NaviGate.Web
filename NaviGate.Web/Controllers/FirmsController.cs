using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using NaviGate.Web.Models.ViewModels;
using NaviGate.Web.ViewModels;
using NaviGate.Web.Models;
namespace NaviGate.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class FirmsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FirmsController> _logger;
        private readonly IFirmRepository _firmRepository;


        public FirmsController(ApplicationDbContext context, 
            UserManager<User> userManager, 
            IWebHostEnvironment webHostEnvironment, 
            ILogger<FirmsController> logger, 
            IFirmRepository firmRepository)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _firmRepository = firmRepository;
        }

        // GET: Firms
        public async Task<IActionResult> Index(string searchString, bool? isActiveFilter, string firmTypeFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Kullanıcı bir şekilde bulunamazsa (nadir bir durum), boş bir sayfa göster.
                return View(new FirmIndexViewModel { Firms = new List<Firm>() });
            }
            // Temel sorgu
            IQueryable<Firm> firmsQuery = _context.Firms;

            // YENİ ROL BAZLI FİLTRELEME
            if (User.IsInRole("Admin"))
            {
                // Admin tüm firmaları görür.
                firmsQuery = _context.Firms;
            }
            else // Manager
            {
                // Manager sadece kendi firmasını görür.
                firmsQuery = _context.Firms.Where(f => f.FirmId == currentUser.FirmId);
            }

            // ARAMA: Firma adına göre
            if (!string.IsNullOrEmpty(searchString))
            {
                firmsQuery = firmsQuery.Where(f => f.FirmName.Contains(searchString));
            }

            // FİLTRELEME: Firma Tipine göre
            if (!string.IsNullOrEmpty(firmTypeFilter))
            {
                // Gelen metni (string) enum türüne çevirmeye çalış.
                if (Enum.TryParse<FirmTypeEnum>(firmTypeFilter, out var parsedFirmType))
                {
                    // Eğer çevirme başarılı olursa, sorguyu bu enum değeriyle yap.
                    firmsQuery = firmsQuery.Where(f => f.FirmType == parsedFirmType);
                }
            }

            // FİLTRELEME: Aktif/Pasif durumuna göre
            if (isActiveFilter.HasValue)
            {
                firmsQuery = firmsQuery.Where(f => f.IsActive == isActiveFilter.Value);
            }

            var firmTypeQuery = _context.Firms.OrderBy(f => f.FirmType).Select(f => f.FirmType).Distinct();

            // ViewModel'i oluştur ve doldur
            var viewModel = new FirmIndexViewModel
            {
                FirmTypes = new SelectList(await firmTypeQuery.ToListAsync(), firmTypeFilter),
                Firms = await firmsQuery.OrderBy(f => f.FirmName).ToListAsync(),
                SearchString = searchString,
                IsActiveFilter = isActiveFilter,
                FirmTypeFilter = firmTypeFilter
            };

            return View(viewModel);
        }

        

        // GET: Firms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var firm = await _context.Firms.FirstOrDefaultAsync(m => m.FirmId == id);
            if (firm == null) return NotFound();

            // GÜVENLİK KONTROLÜ
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && firm.FirmId != currentUser.FirmId)
            {
                return Forbid(); // Eğer Admin değilse ve kendi firması değilse erişimi engelle
            }

            return View(firm);
        }

        // GET: Firms/Create
        [Authorize(Roles = "Admin")] // Yeni firma oluşturmayı SADECE Admin yapabilir.
        public IActionResult Create()
        {
            return View();
        }


        // GET: Firms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var firm = await _context.Firms.FindAsync(id);
            if (firm == null) return NotFound();

            // GÜVENLİK KONTROLÜ
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && firm.FirmId != currentUser.FirmId)
            {
                return Forbid();
            }

            return View(firm);
        }

        // POST: Firms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Firm firm)
        {
            if (id != firm.FirmId) return NotFound();

            // GÜVENLİK KONTROLÜ
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && firm.FirmId != currentUser.FirmId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    firm.ModifiedAtUtc = DateTime.UtcNow;
                    _context.Update(firm);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FirmExists(firm.FirmId)) return NotFound();
                    else throw;
                }

                TempData["SuccessMessage"] = $"'{firm.FirmName}' adlı firma başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(firm);
        }

        // POST: Firms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FirmName,FirmType,PhoneNumber,Email,ContactPerson,Address,Website,TaxNumber,TaxOffice,IsActive,Notes")] Firm firm)
        {
            // Bu satır, Firm modeli üzerindeki [Required], [StringLength] gibi TÜM kuralları
            // tek seferde kontrol eder.
            if (ModelState.IsValid)
            {
                firm.CreatedAtUtc = DateTime.UtcNow; // Oluşturma tarihini sunucu tarafında ata
                _context.Add(firm);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            TempData["SuccessMessage"] = $"'{firm.FirmName}' adlı firma başarıyla oluşturuldu.";
            // Eğer model geçerli değilse (örn: FirmType veya FirmName boşsa),
            // kullanıcıyı girdiği verilerle birlikte aynı sayfaya geri gönder.
            // Create.cshtml'deki <span asp-validation-for... > etiketleri ilgili hatayı gösterecektir.
            return View(firm);
        }

        // GET: Firms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var firm = await _context.Firms
                .FirstOrDefaultAsync(m => m.FirmId == id);
            if (firm == null)
            {
                return NotFound();
            }

            return View(firm);
        }

        // POST: Firms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Bu işlemi sadece Admin yapabilsin
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Silinecek firmayı, ilişkili tüm alt tablolarıyla birlikte buluyoruz.
            var firmToDelete = await _context.Firms
                .Include(f => f.Users)
                .Include(f => f.Shipments)
                    .ThenInclude(s => s.Containers)
                .Include(f => f.Shipments)
                    .ThenInclude(s => s.Documents)
                .FirstOrDefaultAsync(f => f.FirmId == id);

            if (firmToDelete == null)
            {
                TempData["ErrorMessage"] = "Silinecek firma bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // --- MANUEL ZİNCİRLEME SİLME İŞLEMİ ---
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Adım: Sevkiyatlara bağlı dökümanları ve fiziksel dosyaları sil
                    foreach (var shipment in firmToDelete.Shipments)
                    {
                        foreach (var document in shipment.Documents.ToList())
                        {
                            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                            _context.Documents.Remove(document);
                        }
                        // Konteynerler, sevkiyat silinince otomatik silinir (cascade)
                    }
                    // Değişiklikleri ara sıra kaydetmek büyük işlemlerde performansı artırır
                    await _context.SaveChangesAsync();

                    // 2. Adım: Firmaya bağlı sevkiyatların kendisini sil
                    _context.Shipments.RemoveRange(firmToDelete.Shipments);
                    await _context.SaveChangesAsync();

                    // 3. Adım: Firmaya bağlı kullanıcıları UserManager ile güvenli bir şekilde sil
                    foreach (var user in firmToDelete.Users.ToList())
                    {
                        await _userManager.DeleteAsync(user);
                    }

                    // 4. Adım: Artık tüm "çocukları" temizlenen ana firmayı sil
                    _context.Firms.Remove(firmToDelete);
                    await _context.SaveChangesAsync();

                    // Her şey yolunda gittiyse, işlemi onayla
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Firma ve ilişkili tüm verileri başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    // Herhangi bir adımda hata olursa, tüm işlemleri geri al
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Firma silinemedi. Hata: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool FirmExists(int id)
        {
            return _context.Firms.Any(e => e.FirmId == id);
        }
    }
}
