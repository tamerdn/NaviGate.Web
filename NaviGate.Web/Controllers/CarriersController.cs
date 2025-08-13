using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NaviGate.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    public class CarriersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarriersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Carriers
        public async Task<IActionResult> Index(string searchString, bool? isActiveFilter)
        {
            IQueryable<Carrier> carriersQuery = _context.Carriers;

            if (!string.IsNullOrEmpty(searchString))
            {
                carriersQuery = carriersQuery.Where(c => c.CarrierName.Contains(searchString));
            }

            if (isActiveFilter.HasValue)
            {
                carriersQuery = carriersQuery.Where(c => c.IsActive == isActiveFilter.Value);
            }

            var viewModel = new CarrierIndexViewModel
            {
                Carriers = await carriersQuery.OrderBy(c => c.CarrierName).ToListAsync(),
                SearchString = searchString,
                IsActiveFilter = isActiveFilter
            };

            return View(viewModel);
        }

        // GET: Carriers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carrier = await _context.Carriers
                .FirstOrDefaultAsync(m => m.CarrierId == id);
            if (carrier == null)
            {
                return NotFound();
            }

            return View(carrier);
        }

        // GET: Carriers/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Carriers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("CarrierId,CarrierName,ScacCode,Website,TrackingUrl,ContactPerson,Email,PhoneNumber,IsActive,Notes")] Carrier carrier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(carrier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"'{carrier.CarrierName}' adlı taşıyıcı başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            return View(carrier);
        }

        // GET: Carriers/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carrier = await _context.Carriers.FindAsync(id);
            if (carrier == null)
            {
                return NotFound();
            }
            return View(carrier);
        }

        // POST: Carriers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CarrierId,CarrierName,ScacCode,Website,TrackingUrl,ContactPerson,Email,PhoneNumber,IsActive,Notes")] Carrier carrier)
        {
            if (id != carrier.CarrierId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carrier);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarrierExists(carrier.CarrierId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = $"'{carrier.CarrierName}' adlı taşıyıcı başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(carrier);
        }

        // GET: Carriers/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carrier = await _context.Carriers
                .FirstOrDefaultAsync(m => m.CarrierId == id);
            if (carrier == null)
            {
                return NotFound();
            }

            return View(carrier);
        }

        // POST: Carriers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var carrier = await _context.Carriers.FindAsync(id);
            if (carrier != null)
            {
                _context.Carriers.Remove(carrier);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"'{carrier.CarrierName}' adlı taşıyıcı başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        private bool CarrierExists(int id)
        {
            return _context.Carriers.Any(e => e.CarrierId == id);
        }
    }
}
