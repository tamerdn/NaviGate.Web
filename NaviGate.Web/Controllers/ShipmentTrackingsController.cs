using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NaviGate.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    public class ShipmentTrackingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ShipmentTrackingsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ShipmentTrackings
        public async Task<IActionResult> Index()
        {
            // 1. Mevcut kullanıcıyı bul.
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Kullanıcı bulunamazsa veya giriş yapmamışsa
            }

            // 2. Temel sorguyu oluştur.
            IQueryable<Shipment> shipmentsQuery;

            // 3. Kullanıcının rolüne göre sorguyu filtrele.
            if (User.IsInRole("Admin"))
            {
                // Admin tüm sevkiyatları görür.
                shipmentsQuery = _context.Shipments;
            }
            else if (User.IsInRole("Manager"))
            {
                // Manager sadece kendi firmasının sevkiyatlarını görür.
                shipmentsQuery = _context.Shipments.Where(s => s.FirmId == currentUser.FirmId);
            }
            else // User rolündeyse
            {
                // User sadece kendi oluşturduğu sevkiyatları görür.
                shipmentsQuery = _context.Shipments.Where(s => s.CreatedByUserId == currentUser.Id);
            }

            // 4. Filtrelenmiş sorgu üzerinden ilişkili verileri çek ve sırala.
            var shipmentsWithTrackings = await shipmentsQuery
                                             .Include(s => s.Trackings)
                                             .Include(s => s.Firm) // Opsiyonel ama başlıkta faydalı olabilir
                                             .OrderByDescending(s => s.EstimatedDepartureUtc)
                                             .ToListAsync();

            return View(shipmentsWithTrackings);
        }


        // GET: ShipmentTrackings/Create
        public IActionResult Create()
        {
            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ReferenceNumber");
            return View();
        }

        // POST: ShipmentTrackings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShipmentId,Location,StatusDescription,EventDateUtc")] ShipmentTracking shipmentTracking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shipmentTracking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sevkiyat hareketi başarıyla eklendi."; // Başarı mesajı ekleyelim
                return RedirectToAction(nameof(Index));
            }
            // ModelState geçerli değilse, form tekrar gösterilir.
            // Validasyon hatalarını bu sefer asp-validation-for etiketleri gösterecek.
            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ReferenceNumber", shipmentTracking.ShipmentId);
            return View(shipmentTracking);
        }

        // ... (Edit, Details, Delete metotları scaffolding'den geldiği gibi kalabilir) ...
    }
}
