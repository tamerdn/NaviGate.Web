using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NaviGate.Web.Controllers
{
    // Tüm yetkili rollerin erişebilmesi için güncellendi.
    [Authorize(Roles = "Admin,Manager,User")]
    public class AiAlertsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        // UserManager'ı constructor'a ekledik.
        public AiAlertsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: AiAlerts
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<AiAlert> alertsQuery;

            // Rol bazlı filtreleme mantığı
            if (User.IsInRole("Admin"))
            {
                // Admin tüm uyarıları görür.
                alertsQuery = _context.AiAlerts;
            }
            else if (User.IsInRole("Manager"))
            {
                // Manager, kendi firmasına ait sevkiyatların uyarılarını görür.
                var firmShipmentIds = _context.Shipments
                                            .Where(s => s.FirmId == currentUser.FirmId)
                                            .Select(s => s.ShipmentId);
                alertsQuery = _context.AiAlerts.Where(a => firmShipmentIds.Contains(a.ShipmentId));
            }
            else // User
            {
                // User, kendi oluşturduğu sevkiyatların uyarılarını görür.
                var userShipmentIds = _context.Shipments
                                            .Where(s => s.CreatedByUserId == currentUser.Id)
                                            .Select(s => s.ShipmentId);
                alertsQuery = _context.AiAlerts.Where(a => userShipmentIds.Contains(a.ShipmentId));
            }

            // Uyarıları önce çözülmemiş olanlar, sonra en yeni olanlar üste gelecek şekilde sırala.
            var alerts = await alertsQuery
                                .Include(a => a.Shipment)
                                .OrderBy(a => a.IsResolved) // Önce çözülmemişler (false)
                                .ThenByDescending(a => a.CreatedAtUtc) // Sonra tarihe göre
                                .ToListAsync();

            // DEĞİŞİKLİK BURADA: Düz listeyi, sevkiyata göre gruplanmış bir listeye çeviriyoruz.
            var alertsByShipment = alerts
                                    .GroupBy(a => a.Shipment)
                                    .OrderByDescending(g => g.Any(a => !a.IsResolved)) // Önce çözülmemiş uyarısı olanlar
                                    .ThenByDescending(g => g.Key.CreatedAtUtc); // Sonra en yeni sevkiyatlar

            return View(alertsByShipment);
        }

        // POST: AiAlerts/Resolve/5
        // YENİ EKLENEN METOT: Bir uyarıyı "Çözüldü" olarak işaretler.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var alert = await _context.AiAlerts.Include(a => a.Shipment).FirstOrDefaultAsync(a => a.AiAlertId == id);
            if (alert == null)
            {
                return NotFound();
            }

            // Güvenlik kontrolü: Kullanıcı bu uyarıyı çözebilir mi?
            if (!await IsUserAuthorizedForAlert(alert))
            {
                return Forbid();
            }

            alert.IsResolved = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Uyarı 'Çözüldü' olarak işaretlendi.";
            return RedirectToAction(nameof(Index));
        }


        // GET: AiAlerts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var aiAlert = await _context.AiAlerts.Include(a => a.Shipment).FirstOrDefaultAsync(m => m.AiAlertId == id);
            if (aiAlert == null) return NotFound();

            // Güvenlik kontrolü
            if (!await IsUserAuthorizedForAlert(aiAlert)) return Forbid();

            return View(aiAlert);
        }

        // POST: AiAlerts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aiAlert = await _context.AiAlerts.Include(a => a.Shipment).FirstOrDefaultAsync(a => a.AiAlertId == id);
            if (aiAlert != null)
            {
                // Güvenlik kontrolü
                if (!await IsUserAuthorizedForAlert(aiAlert)) return Forbid();

                _context.AiAlerts.Remove(aiAlert);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Uyarı başarıyla silindi.";
            }
            return RedirectToAction(nameof(Index));
        }

        // YENİ EKLENEN YARDIMCI METOT: Tekrarlanan yetki kontrolü kodunu merkezileştirir.
        private async Task<bool> IsUserAuthorizedForAlert(AiAlert alert)
        {
            if (User.IsInRole("Admin")) return true;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return false;

            if (User.IsInRole("Manager"))
            {
                return alert.Shipment.FirmId == currentUser.FirmId;
            }

            // User
            return alert.Shipment.CreatedByUserId == currentUser.Id;
        }
    }
}