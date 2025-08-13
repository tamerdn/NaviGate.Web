using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NaviGate.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    public class ContainersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ContainersController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Containers
        public async Task<IActionResult> Index(string searchString, int? shipmentIdFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Temel sorgu, sevkiyat bilgisini de dahil ediyoruz
            IQueryable<Container> containersQuery = _context.Containers.Include(c => c.Shipment);

            // Rol bazlı filtreleme
            if (User.IsInRole("Admin"))
            {
                // Admin tüm konteynerleri görür
                containersQuery = _context.Containers;
            }
            else if (User.IsInRole("Manager"))
            {
                // Manager, kendi firmasına ait sevkiyatların konteynerlerini görür
                var firmShipmentIds = _context.Shipments
                                            .Where(s => s.FirmId == currentUser.FirmId)
                                            .Select(s => s.ShipmentId);
                containersQuery = _context.Containers.Where(c => firmShipmentIds.Contains(c.ShipmentId));
            }
            else // User
            {
                // User, kendi oluşturduğu sevkiyatların konteynerlerini görür
                var userShipmentIds = _context.Shipments
                                            .Where(s => s.CreatedByUserId == currentUser.Id)
                                            .Select(s => s.ShipmentId);
                containersQuery = _context.Containers.Where(c => userShipmentIds.Contains(c.ShipmentId));
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                containersQuery = containersQuery.Where(c => c.ContainerNumber.Contains(searchString));
            }

            if (shipmentIdFilter.HasValue)
            {
                containersQuery = containersQuery.Where(c => c.ShipmentId == shipmentIdFilter.Value);
            }

            var viewModel = new ContainerIndexViewModel
            {
                Shipments = new SelectList(await _context.Shipments.OrderBy(s => s.ReferenceNumber).ToListAsync(), "ShipmentId", "ReferenceNumber", shipmentIdFilter),
                Containers = await containersQuery.OrderByDescending(c => c.ContainerId).ToListAsync(),
                SearchString = searchString,
                ShipmentIdFilter = shipmentIdFilter
            };

            return View(viewModel);
        }

        // GET: Containers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var container = await _context.Containers
                .Include(c => c.Shipment)
                .FirstOrDefaultAsync(m => m.ContainerId == id);
            if (container == null)
            {
                return NotFound();
            }

            return View(container);
        }

        // GET: Containers/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ContainerCreateViewModel();
            await PopulateAuthorizedShipmentsDropdown(viewModel);
            return View(viewModel);
        }

        // POST: Containers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContainerCreateViewModel viewModel)
        {
            // GÜVENLİK KONTROLÜ: Kullanıcı bu sevkiyata konteyner ekleyebilir mi?
            var shipment = await _context.Shipments.FindAsync(viewModel.ShipmentId);
            if (!await IsUserAuthorizedForShipment(shipment))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var container = viewModel.Container;
                container.ShipmentId = viewModel.ShipmentId; // Seçilen sevkiyatı ata
                _context.Add(container);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Konteyner başarıyla oluşturuldu.";
                return RedirectToAction("Details", "Shipments", new { id = container.ShipmentId });
            }

            await PopulateAuthorizedShipmentsDropdown(viewModel);
            return View(viewModel);
        }

        // GET: Containers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var container = await _context.Containers.Include(c => c.Shipment).FirstOrDefaultAsync(c => c.ContainerId == id);
            if (container == null) return NotFound();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu konteyneri düzenleyebilir mi?
            if (!await IsUserAuthorizedForShipment(container.Shipment))
            {
                return Forbid();
            }

            var viewModel = new ContainerEditViewModel
            {
                Container = container,
                Shipment = container.Shipment
            };
            return View(viewModel);
        }

        // POST: Containers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContainerEditViewModel viewModel)
        {
            if (id != viewModel.Container.ContainerId) return NotFound();

            var containerToUpdate = await _context.Containers.Include(c => c.Shipment).FirstOrDefaultAsync(c => c.ContainerId == id);
            if (containerToUpdate == null) return NotFound();

            // GÜVENLİK KONTROLÜ: Kullanıcı bu konteyneri düzenleyebilir mi?
            if (!await IsUserAuthorizedForShipment(containerToUpdate.Shipment))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Gelen verileri veritabanındaki nesneye aktar
                _context.Entry(containerToUpdate).CurrentValues.SetValues(viewModel.Container);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Konteyner başarıyla güncellendi.";
                return RedirectToAction("Details", "Shipments", new { id = containerToUpdate.ShipmentId });
            }

            viewModel.Shipment = containerToUpdate.Shipment; // Hata durumunda sevkiyat bilgisini tekrar doldur
            return View(viewModel);
        }

        // GET: Containers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var container = await _context.Containers
                .Include(c => c.Shipment)
                .FirstOrDefaultAsync(m => m.ContainerId == id);
            if (container == null)
            {
                return NotFound();
            }

            return View(container);
        }

        // POST: Containers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var container = await _context.Containers.FindAsync(id);
            if (container != null)
            {
                _context.Containers.Remove(container);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"'{container.ContainerNumber}' numaralı taşıyıcı başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        // --- YARDIMCI METOTLAR ---
        private async Task PopulateAuthorizedShipmentsDropdown(ContainerCreateViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            IQueryable<Shipment> authorizedShipmentsQuery;

            if (User.IsInRole("Admin"))
            {
                authorizedShipmentsQuery = _context.Shipments;
            }
            else if (User.IsInRole("Manager"))
            {
                authorizedShipmentsQuery = _context.Shipments.Where(s => s.FirmId == currentUser.FirmId);
            }
            else // User
            {
                authorizedShipmentsQuery = _context.Shipments.Where(s => s.CreatedByUserId == currentUser.Id);
            }

            viewModel.AuthorizedShipments = new SelectList(
                await authorizedShipmentsQuery.OrderBy(s => s.ReferenceNumber).ToListAsync(),
                "ShipmentId", "ReferenceNumber", viewModel.ShipmentId);
        }

        private async Task<bool> IsUserAuthorizedForShipment(Shipment shipment)
        {
            if (shipment == null) return false;
            if (User.IsInRole("Admin")) return true;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return false;

            if (User.IsInRole("Manager"))
            {
                return shipment.FirmId == currentUser.FirmId;
            }

            // User
            return shipment.CreatedByUserId == currentUser.Id;
        }
    
        private bool ContainerExists(int id)
        {
            return _context.Containers.Any(e => e.ContainerId == id);
        }
    }
}
