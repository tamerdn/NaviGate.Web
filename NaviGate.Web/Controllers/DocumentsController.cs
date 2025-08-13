using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.Services;
using NaviGate.Web.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NaviGate.Web.Helpers;

namespace NaviGate.Web.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DocumentsController> _logger;
        private readonly IVirusScannerService _virusScannerService;

        public DocumentsController(
            ApplicationDbContext context, 
            UserManager<User> userManager, 
            IWebHostEnvironment webHostEnvironment, 
            ILogger<DocumentsController> logger,
            IVirusScannerService virusScannerService)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _virusScannerService = virusScannerService;
        }

        // GET: Documents
        [Authorize(Roles = "Admin,Manager,User")]
        public async Task<IActionResult> Index(string searchString, int? shipmentIdFilter, DocumentType? DocumentTypeFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<Document> documentsQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin tüm dökümanları görür
                documentsQuery = _context.Documents;
            }
            else if (User.IsInRole("Manager"))
            {
                // Manager, kendi firmasına ait sevkiyatların dökümanlarını görür
                var firmShipmentIds = _context.Shipments
                    .Where(s => s.FirmId == currentUser.FirmId)
                    .Select(s => s.ShipmentId);
                documentsQuery = _context.Documents.Where(d => firmShipmentIds.Contains(d.ShipmentId));
            }
            else // User
            {
                // User, sadece kendi yüklediği dökümanları görür
                documentsQuery = _context.Documents.Where(d => d.UploadedByUserId == currentUser.Id);
            }

            // ARAMA: Dosya adına göre
            if (!string.IsNullOrEmpty(searchString))
            {
                documentsQuery = documentsQuery.Where(d => d.FileName.Contains(searchString));
            }
            // FİLTRELEME: Sevkiyata göre
            if (shipmentIdFilter.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.ShipmentId == shipmentIdFilter.Value);
            }
            // FİLTRELEME: Döküman Tipine göre
            if (DocumentTypeFilter.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.DocumentType == DocumentTypeFilter.Value);
            }

            var viewModel = new DocumentIndexViewModel
            {
                // Dropdown'ları doldur
                Shipments = new SelectList(await _context.Shipments.OrderBy(s => s.ReferenceNumber).ToListAsync(), "ShipmentId", "ReferenceNumber", shipmentIdFilter),
                DocTypes = new SelectList(Enum.GetValues(typeof(DocumentType)).Cast<DocumentType>().Select(v => new SelectListItem
                {
                    Text = v.GetDisplayName(),
                    Value = v.ToString()
                }).ToList(), "Value", "Text", DocumentTypeFilter),

                // Filtrelenmiş listeyi al
                Documents = await documentsQuery
                                .Include(d => d.Shipment)
                                .Include(d => d.UploadedByUser)
                                .OrderByDescending(d => d.UploadDateUtc)
                                .ToListAsync(),

                // Mevcut filtreleri View'a geri gönder
                SearchString = searchString,
                ShipmentIdFilter = shipmentIdFilter,
                DocumentTypeFilter = DocumentTypeFilter
            };
            return View(viewModel);
        }

        // GET: Documents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var document = await _context.Documents
                .Include(d => d.Shipment)
                .Include(d => d.UploadedByUser)
                .FirstOrDefaultAsync(m => m.DocumentId == id);
            if (document == null) return NotFound();

            await AuthorizeDocumentAccess(document);
            return View(document);
        }

        // Get: Documents/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Shipment) // Sevkiyat bilgisini de dahil et
                .FirstOrDefaultAsync(d => d.DocumentId == id);
            if (document == null) return NotFound();

            var viewModel = new DocumentEditViewModel
            {
                DocumentId = document.DocumentId, // Id yerine DocumentId
                ShipmentId = document.ShipmentId,
                DocumentType = document.DocumentType,
                VerificationStatus = document.VerificationStatus,
                VerificationNotes = document.VerificationNotes,
                FileName = document.FileName, // ExistingFileName yerine FileName
                UploadedByUserId = document.UploadedByUserId
            };
            ViewBag.ShipmentReferenceNumber = document.Shipment?.ReferenceNumber ?? "Bulunamadı";

            return View(viewModel);
        }


        // Post: Documents/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentEditViewModel model)
        {
            if (id != model.DocumentId) return NotFound();

            if (ModelState.IsValid)
            {
                var documentToUpdate = await _context.Documents.FindAsync(id);
                if (documentToUpdate == null) return NotFound();

                // Yeni dosya yüklendi mi?
                if (model.NewDocumentFile != null && model.NewDocumentFile.Length > 0)
                {
                    // 1. DOSYA GÜVENLİK KONTROLLERİ (Create'dekiyle aynı)
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
                    var dangerousExtensions = new[] { ".py", ".exe", ".js", ".php", ".ps1", ".bat", ".cmd" };

                    var fileExt = Path.GetExtension(model.NewDocumentFile.FileName).ToLowerInvariant();

                    // Uzantı kontrolü
                    if (dangerousExtensions.Contains(fileExt) || !allowedExtensions.Contains(fileExt))
                    {
                        ModelState.AddModelError("NewDocumentFile", "İzin verilmeyen dosya türü!");
                        await PopulateShipmentsDropdown(model.ShipmentId);
                        return View(model);
                    }

                    // MIME Type kontrolü
                    var allowedMimeTypes = new[]
                    {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

                    if (!allowedMimeTypes.Contains(model.NewDocumentFile.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("NewDocumentFile", "Geçersiz dosya formatı!");
                        await PopulateShipmentsDropdown(model.ShipmentId);
                        return View(model);
                    }

                    // Dosya boyutu kontrolü (25MB)
                    if (model.NewDocumentFile.Length > 25 * 1024 * 1024)
                    {
                        ModelState.AddModelError("NewDocumentFile", "Dosya boyutu 25MB'ı geçemez!");
                        await PopulateShipmentsDropdown(model.ShipmentId);
                        return View(model);
                    }

                    // Magic Number kontrolü
                    if (!await VerifyFileSignature(model.NewDocumentFile, fileExt))
                    {
                        ModelState.AddModelError("NewDocumentFile", "Dosya içeriği uzantıyla uyuşmuyor!");
                        await PopulateShipmentsDropdown(model.ShipmentId);
                        return View(model);
                    }

                    // 2. VİRÜS TARAMASI
                    var tempFilePath = Path.GetTempFileName();
                    await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await model.NewDocumentFile.CopyToAsync(stream);
                    }

                    var scanResult = await _virusScannerService.ScanAsync(tempFilePath);
                    if (scanResult.IsThreatDetected)
                    {
                        System.IO.File.Delete(tempFilePath);
                        ModelState.AddModelError("NewDocumentFile", $"Virüs tehdidi: {scanResult.ThreatType}");
                        await PopulateShipmentsDropdown(model.ShipmentId);
                        return View(model);
                    }

                    // 3. ESKİ DOSYAYI SİL
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        documentToUpdate.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    // 4. YENİ DOSYAYI KAYDET
                    var fileName = Guid.NewGuid().ToString() + fileExt;
                    var newFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "documents", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                    System.IO.File.Move(tempFilePath, newFilePath);

                    // 5. VERİTABANI GÜNCELLEME
                    documentToUpdate.FilePath = "/documents/" + fileName;
                    documentToUpdate.FileName = model.NewDocumentFile.FileName;
                    documentToUpdate.FileSizeInBytes = model.NewDocumentFile.Length;
                    documentToUpdate.MimeType = model.NewDocumentFile.ContentType;
                }

                // Diğer alanları güncelle
                documentToUpdate.DocumentType = model.DocumentType;
                documentToUpdate.VerificationStatus = model.VerificationStatus;
                documentToUpdate.VerificationNotes = model.VerificationNotes;

                try
                {
                    _context.Update(documentToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Döküman başarıyla güncellendi!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentExists(model.DocumentId))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateShipmentsDropdown(model.ShipmentId);
            return View(model);
        }



        // GET: Documents/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new DocumentCreateViewModel();
            await PopulateShipmentsDropdown(viewModel);
            return View(viewModel);
        }

        // POST: Documents/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var documentFile = viewModel.DocumentFile;
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents");
                Directory.CreateDirectory(uploadPath);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + documentFile.FileName;
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }

                var document = new Document
                {
                    ShipmentId = viewModel.ShipmentId,
                    DocumentType = viewModel.DocumentType,
                    FilePath = "/documents/" + uniqueFileName,
                    FileName = documentFile.FileName,
                    FileSizeInBytes = documentFile.Length,
                    MimeType = documentFile.ContentType,
                    UploadDateUtc = DateTime.UtcNow,
                    UploadedByUserId = _userManager.GetUserId(User),
                    VerificationStatus = "Onay Bekliyor"
                };
                _context.Add(document);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Döküman başarıyla yüklendi!";
                return RedirectToAction("Details", "Shipments", new { id = document.ShipmentId });
            }

            await PopulateShipmentsDropdown(viewModel);
            return View(viewModel);
        }

        // GET: Documents/Download/5
        public async Task<IActionResult> Download(int? id)
        {
            if (id == null) return NotFound();
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            await AuthorizeDocumentAccess(document);

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Sunucuda dosya bulunamadı.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, document.MimeType, document.FileName);
        }

        // --- YARDIMCI METOTLAR ---
        private async Task PopulateShipmentsDropdown(DocumentCreateViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Shipment> shipmentsQuery = User.IsInRole("Admin")
                ? _context.Shipments
                : _context.Shipments.Where(s => s.FirmId == user.FirmId);
            if (User.IsInRole("Admin"))
            {
                shipmentsQuery = _context.Shipments;
            }
            else if (User.IsInRole("Manager"))
            {
                shipmentsQuery = _context.Shipments.Where(s => s.FirmId == user.FirmId);
            }
            else // User
            {
                shipmentsQuery = _context.Shipments.Where(s => s.CreatedByUserId == user.Id);
            }
            viewModel.ShipmentOptions = new SelectList(
                await shipmentsQuery.AsNoTracking().ToListAsync(),
                "ShipmentId", "ReferenceNumber", viewModel.ShipmentId);
        }

        private async Task PopulateShipmentsDropdown(int? shipmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Shipment> shipmentsQuery = User.IsInRole("Admin")
                ? _context.Shipments
                : _context.Shipments.Where(s => s.FirmId == user.FirmId);

            ViewBag.ShipmentOptions = new SelectList(
                await shipmentsQuery.AsNoTracking().ToListAsync(),
                "ShipmentId", "ReferenceNumber", shipmentId);
        }

        private async Task AuthorizeDocumentAccess(Document document)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var shipment = await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.ShipmentId == document.ShipmentId);
            if (!User.IsInRole("Admin") && (shipment == null || shipment.FirmId != currentUser.FirmId))
            {
                HttpContext.Response.StatusCode = 403; // Forbidden
                await HttpContext.Response.WriteAsync("Access Denied");
            }
        }

        private async Task<bool> VerifyFileSignature(IFormFile file, string fileExtension)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();

                // PDF kontrolü
                if (fileExtension == ".pdf" && bytes.Length > 4 &&
                    bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                    return true;

                // JPEG kontrolü
                if ((fileExtension == ".jpg" || fileExtension == ".jpeg") && bytes.Length > 2 &&
                    bytes[0] == 0xFF && bytes[1] == 0xD8)
                    return true;

                // PNG kontrolü
                if (fileExtension == ".png" && bytes.Length > 4 &&
                    bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                    return true;

                // Diğer dosya türleri için kontroller ekleyebilirsiniz

                return false;
            }
            catch
            {
                return false;
            }
        }

        

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.DocumentId == id);
        }

        
    }
}