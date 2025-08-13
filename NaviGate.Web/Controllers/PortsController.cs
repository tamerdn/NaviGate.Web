using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using NaviGate.Web.ViewModels;


namespace NaviGate.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    public class PortsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PortsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ports
        public async Task<IActionResult> Index(string searchString, string countryFilter)
        {
            // Temel sorgu
            IQueryable<Port> portsQuery = _context.Ports;

            // ARAMA: Liman Adı veya Liman Kodu'na göre
            if (!string.IsNullOrEmpty(searchString))
            {
                portsQuery = portsQuery.Where(p => p.PortName.Contains(searchString) || p.PortCode.Contains(searchString));
            }

            // FİLTRELEME: Ülkeye göre
            if (!string.IsNullOrEmpty(countryFilter))
            {
                portsQuery = portsQuery.Where(p => p.Country == countryFilter);
            }

            // Ülke dropdown'ını doldurmak için veritabanındaki tüm benzersiz ülkeleri çek
            var countryList = await _context.Ports
                                        .OrderBy(p => p.Country)
                                        .Select(p => p.Country)
                                        .Distinct()
                                        .ToListAsync();

            // ViewModel'i oluştur ve doldur
            var viewModel = new PortIndexViewModel
            {
                // Dropdown için ülke listesini hazırla
                Countries = new SelectList(countryList, countryFilter),

                // Filtrelenmiş ve sıralanmış liman listesini al
                Ports = await portsQuery.OrderBy(p => p.PortName).ToListAsync(),

                // Mevcut filtre değerlerini View'a geri gönder ki form dolu kalsın
                SearchString = searchString,
                CountryFilter = countryFilter
            };

            return View(viewModel);
        }

        // GET: Ports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var port = await _context.Ports
                .FirstOrDefaultAsync(m => m.PortId == id);
            if (port == null)
            {
                return NotFound();
            }

            return View(port);
        }

        // GET: Ports/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("PortId,PortName,PortCode,Country")] Port port)
        {
            if (ModelState.IsValid)
            {
                _context.Add(port);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"'{port.PortName}' adlı liman başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            return View(port);
        }

        // GET: Ports/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var port = await _context.Ports.FindAsync(id);
            if (port == null)
            {
                return NotFound();
            }
            return View(port);
        }

        // POST: Ports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("PortId,PortName,PortCode,Country")] Port port)
        {
            if (id != port.PortId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(port);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PortExists(port.PortId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = $"'{port.PortName}' adlı liman başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(port);
        }

        // GET: Ports/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var port = await _context.Ports
                .FirstOrDefaultAsync(m => m.PortId == id);
            if (port == null)
            {
                return NotFound();
            }

            return View(port);
        }

        // POST: Ports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var port = await _context.Ports.FindAsync(id);
            if (port != null)
            {
                _context.Ports.Remove(port);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"'{port.PortName}' adlı liman başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        private bool PortExists(int id)
        {
            return _context.Ports.Any(e => e.PortId == id);
        }
    }
}
