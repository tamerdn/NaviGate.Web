using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace NaviGate.Web.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public NotificationViewComponent(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Kullanıcı giriş yapmamışsa, hiçbir şey gösterme.
            if (!User.Identity.IsAuthenticated)
            {
                return Content(string.Empty);
            }

            var currentUser = await _userManager.GetUserAsync((System.Security.Claims.ClaimsPrincipal)User);
            if (currentUser == null)
            {
                return Content(string.Empty);
            }

            IQueryable<AiAlert> alertsQuery;

            // AiAlertsController'daki ile aynı rol bazlı filtreleme mantığı
            if (User.IsInRole("Admin"))
            {
                alertsQuery = _context.AiAlerts;
            }
            else if (User.IsInRole("Manager"))
            {
                var firmShipmentIds = _context.Shipments.Where(s => s.FirmId == currentUser.FirmId).Select(s => s.ShipmentId);
                alertsQuery = _context.AiAlerts.Where(a => firmShipmentIds.Contains(a.ShipmentId));
            }
            else // User
            {
                var userShipmentIds = _context.Shipments.Where(s => s.CreatedByUserId == currentUser.Id).Select(s => s.ShipmentId);
                alertsQuery = _context.AiAlerts.Where(a => userShipmentIds.Contains(a.ShipmentId));
            }

            // Sadece çözülmemiş uyarılara odaklan
            var unresolvedAlertsQuery = alertsQuery.Where(a => !a.IsResolved);

            // Hem TOPLAM sayıyı hem de en son 5 uyarıyı al
            var totalUnresolvedCount = await unresolvedAlertsQuery.CountAsync();
            var recentAlerts = await unresolvedAlertsQuery
                                        .OrderByDescending(a => a.CreatedAtUtc)
                                        .Take(5)
                                        .Include(a => a.Shipment) // Sevkiyat bilgisi de gelsin
                                        .ToListAsync();

            // Verileri tek bir ViewModel içinde paketle
            var viewModel = new NotificationViewModel
            {
                UnresolvedCount = totalUnresolvedCount,
                RecentUnresolvedAlerts = recentAlerts
            };

            return View(viewModel);
        }
    }
}
