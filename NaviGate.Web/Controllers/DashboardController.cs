using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NaviGate.Web.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var viewModel = new DashboardViewModel();

            // 1. Rol bazlı temel sorguları hazırla
            IQueryable<Shipment> userShipmentsQuery;
            IQueryable<AiAlert> userAlertsQuery;

            if (User.IsInRole("Admin"))
            {
                userShipmentsQuery = _context.Shipments;
                userAlertsQuery = _context.AiAlerts;
            }
            else if (User.IsInRole("Manager"))
            {
                userShipmentsQuery = _context.Shipments.Where(s => s.FirmId == currentUser.FirmId);
                var firmShipmentIds = userShipmentsQuery.Select(s => s.ShipmentId);
                userAlertsQuery = _context.AiAlerts.Where(a => firmShipmentIds.Contains(a.ShipmentId));
            }
            else // User
            {
                userShipmentsQuery = _context.Shipments.Where(s => s.CreatedByUserId == currentUser.Id);
                var userShipmentIds = userShipmentsQuery.Select(s => s.ShipmentId);
                userAlertsQuery = _context.AiAlerts.Where(a => userShipmentIds.Contains(a.ShipmentId));
            }

            viewModel.UserFirstName = currentUser.FirstName;
            viewModel.UserLastName = currentUser.LastName;
            var roles = await _userManager.GetRolesAsync(currentUser);
            viewModel.UserRole = roles.FirstOrDefault() ?? "Kullanıcı"; // İlk rolünü al, yoksa varsayılan ata

            // 2. ViewModel'i bu sorgular üzerinden doldur
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var activeStatuses = new[] { ShipmentStatus.ReadyForDispatch, ShipmentStatus.InTransit, ShipmentStatus.AtCustoms };

            viewModel.UnresolvedAlertsCount = await userAlertsQuery.CountAsync(a => !a.IsResolved);
            viewModel.ActiveShipmentsCount = await userShipmentsQuery.CountAsync(s => activeStatuses.Contains(s.Status));
            viewModel.CompletedShipmentsLast30DaysCount = await userShipmentsQuery.CountAsync(s => s.Status == ShipmentStatus.Completed && s.CreatedAtUtc >= thirtyDaysAgo);

            viewModel.PendingDocumentsCount = await _context.Documents
                .Where(d => userShipmentsQuery.Any(s => s.ShipmentId == d.ShipmentId)) // Sadece yetkili olunan sevkiyatların dökümanları
                .CountAsync(d => d.VerificationStatus == "Onay Bekliyor");

            viewModel.RecentShipments = await userShipmentsQuery
                .OrderByDescending(s => s.CreatedAtUtc)
                .Take(5)
                .ToListAsync();

            // Son hareketler için sevkiyatları da dahil etmeliyiz
            var recentTrackingsQuery = _context.ShipmentTrackings
                .Include(t => t.Shipment)
                .OrderByDescending(t => t.EventDateUtc);

            // Son hareketleri de role göre filtrele
            viewModel.RecentTrackings = await recentTrackingsQuery
                .Where(t => userShipmentsQuery.Any(s => s.ShipmentId == t.ShipmentId))
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }
    }
}