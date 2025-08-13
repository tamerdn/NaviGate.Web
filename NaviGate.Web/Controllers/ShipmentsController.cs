using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Helpers;
using NaviGate.Web.Models;
using NaviGate.Web.ViewModels;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NaviGate.Web.Models.ViewModels;

namespace NaviGate.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    public class ShipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ShipmentsController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Shipments
        public async Task<IActionResult> Index(string searchString, int? carrierIdFilter, ShipmentStatus? statusFilter)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Temel sorgu: Her zaman rol bazlı filtreleme ile başlar.
            IQueryable<Shipment> shipmentsQuery;
            if (User.IsInRole("Admin")) { shipmentsQuery = _context.Shipments; }
            else if (User.IsInRole("Manager")) { shipmentsQuery = _context.Shipments.Where(s => s.FirmId == currentUser.FirmId); }
            else { shipmentsQuery = _context.Shipments.Where(s => s.CreatedByUserId == currentUser.Id); }

            // 1. ARAMA: Referans numarasına göre arama
            if (!string.IsNullOrEmpty(searchString))
            {
                shipmentsQuery = shipmentsQuery.Where(s => s.ReferenceNumber.Contains(searchString));
            }

            // 2. FİLTRELEME: Taşıyıcı firmaya göre filtreleme
            if (carrierIdFilter.HasValue)
            {
                shipmentsQuery = shipmentsQuery.Where(s => s.CarrierId == carrierIdFilter.Value);
            }

            // 3. FİLTRELEME: Duruma göre filtreleme
            if (statusFilter.HasValue)
            {
                shipmentsQuery = shipmentsQuery.Where(s => s.Status == statusFilter.Value);
            }

            // Sonuçları ve filtreleme seçeneklerini ViewModel içinde topla
            var viewModel = new ShipmentIndexViewModel
            {
                // Dropdown'ları doldurmak için verileri hazırla
                Carriers = new SelectList(await _context.Carriers.OrderBy(c => c.CarrierName).ToListAsync(), "CarrierId", "CarrierName", carrierIdFilter),
                Statuses = new SelectList(Enum.GetValues(typeof(ShipmentStatus)).Cast<ShipmentStatus>().Select(v => new SelectListItem
                {
                    Text = v.GetDisplayName(),
                    Value = v.ToString()
                }).ToList(), "Value", "Text", statusFilter),

                // Filtrelenmiş ve sıralanmış sevkiyat listesini al
                Shipments = await shipmentsQuery
                                .Include(s => s.Firm)
                                .Include(s => s.Carrier)
                                .Include(s => s.DeparturePort)
                                .Include(s => s.ArrivalPort)
                                .Include(s => s.CreatedByUser)
                                .OrderByDescending(s => s.CreatedAtUtc)
                                .ToListAsync(),

                // Mevcut filtre değerlerini View'a geri gönder ki form dolu kalsın
                SearchString = searchString,
                CarrierIdFilter = carrierIdFilter,
                StatusFilter = statusFilter
            };

            // ... UserRoles'u ViewData'ya ekleme kodunuz aynı kalabilir ...
            var userIds = viewModel.Shipments.Select(s => s.CreatedByUserId).Distinct().ToList();
            var userRoles = await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .ToDictionaryAsync(x => x.UserId, x => x.Name);
            ViewData["UserRoles"] = userRoles;

            return View(viewModel);
        }


        // GET: Shipments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var shipment = await _context.Shipments
                .Include(s => s.Firm).Include(s => s.Carrier)
                .Include(s => s.DeparturePort).Include(s => s.ArrivalPort)
                .Include(s => s.CreatedByUser).Include(s => s.Containers)
                .Include(s => s.Documents)
                .ThenInclude(d => d.UploadedByUser)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);

            if (shipment == null) return NotFound();

            // Yetki kontrolü: Kullanıcı bu sevkiyatı görebilir mi?
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && shipment.FirmId != currentUser.FirmId)
            {
                return Forbid(); // Kendi firması değilse erişimi engelle
            }

            return View(shipment);
        }

        // GET: Shipments/Create
        [Authorize(Roles = "Admin,Manager,User")] // User'ın da erişebilmesi için güncellendi
        public IActionResult Create()
        {
            var viewModel = new ShipmentCreateViewModel();
            PopulateCreateDropdowns(viewModel);
            return View(viewModel);
        }

        // POST: Shipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,User")] // Tüm roller oluşturabilir
        public async Task<IActionResult> Create(ShipmentCreateViewModel viewModel)
        {
            // Referans numarasının benzersiz olup olmadığını en başta kontrol et
            if (!string.IsNullOrEmpty(viewModel.Shipment.ReferenceNumber))
            {
                var referenceExists = await _context.Shipments.AnyAsync(s => s.ReferenceNumber == viewModel.Shipment.ReferenceNumber);
                if (referenceExists)
                {
                    ModelState.AddModelError("Shipment.ReferenceNumber", "Bu referans numarası zaten kullanılıyor.");
                }
            }

            if (ModelState.IsValid)
            {
                var shipmentToCreate = viewModel.Shipment;
                var currentUser = await _userManager.GetUserAsync(User);

                // --- GÜVENLİK GÜNCELLEMESİ BURADA ---

                // Sunucu tarafında atanması zorunlu olan alanlar
                shipmentToCreate.CreatedByUserId = currentUser.Id;
                shipmentToCreate.CreatedAtUtc = DateTime.UtcNow;

                // Rol bazlı FirmId ataması. Bu, formdan ne gelirse gelsin üzerine yazar.
                if (User.IsInRole("Admin"))
                {
                    // Admin, formda hangi firmayı seçtiyse o kullanılır.
                    // viewModel.Shipment.FirmId'ye dokunmuyoruz.
                }
                else
                {
                    // Eğer Admin değilse (Manager veya User ise),
                    // sevkiyat HER ZAMAN kendi firmasına atanır.
                    shipmentToCreate.FirmId = currentUser.FirmId;
                }
                // ------------------------------------

                _context.Add(shipmentToCreate);
                await _context.SaveChangesAsync();

                if (viewModel.Containers != null)
                {
                    foreach (var container in viewModel.Containers)
                    {
                        if (!string.IsNullOrEmpty(container.ContainerNumber))
                        {
                            container.ShipmentId = shipmentToCreate.ShipmentId;
                            _context.Containers.Add(container);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Dökümanları işle
                if (viewModel.Documents != null && viewModel.Documents.Any())
                {
                    foreach (var doc in viewModel.Documents)
                    {
                        if (doc.DocumentFile != null && doc.DocumentFile.Length > 0)
                        {
                            var newDocument = await SaveDocumentFile(
                                doc.DocumentFile,
                                shipmentToCreate.ShipmentId,
                                doc.DocumentType);

                            _context.Documents.Add(newDocument);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                TempData["SuccessMessage"] = "Sevkiyat başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }

            // Hata durumunda formu ve dropdown'ları tekrar hazırla
            PopulateCreateDropdowns(viewModel);
            return View(viewModel);
        }
        // GET: Shipments/Edit/5
        [Authorize(Roles = "Admin,Manager,User")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var shipment = await _context.Shipments
              .Include(s => s.Containers)
              .Include(s => s.Documents)
              .AsNoTracking()
              .FirstOrDefaultAsync(s => s.ShipmentId == id);

            if (shipment == null) return NotFound();

            // ... (Yetki Kontrolleri Aynı Kalacak) ...

            var viewModel = new ShipmentEditViewModel
            {
                Shipment = shipment,
                Containers = shipment.Containers.ToList(),
                // DİKKAT: Gerçek dökümanları ViewModel'e dönüştürüyoruz
                Documents = shipment.Documents.Select(d => new DocumentEditViewModel
                {
                    DocumentId = d.DocumentId,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    UploadedByUserId = d.UploadedByUserId,
                    ShipmentId = d.ShipmentId,
                    VerificationStatus = d.VerificationStatus
                }).ToList()
            };

            PopulateEditDropdowns(viewModel);
            return View(viewModel);
        }

        // POST: Shipments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,User")]
        public async Task<IActionResult> Edit(int id, ShipmentEditViewModel viewModel)
        {
            if (id != viewModel.Shipment.ShipmentId)
            {
                return NotFound();
            }
            var shipmentToUpdate = await _context.Shipments
              .Include(s => s.Containers)
              .Include(s => s.Documents)
              .FirstOrDefaultAsync(s => s.ShipmentId == id);

            if (shipmentToUpdate == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && shipmentToUpdate.FirmId != currentUser.FirmId)
            {
                return Forbid();
            }
            if (ModelState.IsValid)

            {
                // Sevkiyat bilgilerini güncelle

                shipmentToUpdate.ReferenceNumber = viewModel.Shipment.ReferenceNumber;
                shipmentToUpdate.Status = viewModel.Shipment.Status;
                shipmentToUpdate.ReferenceNumber = viewModel.Shipment.ReferenceNumber;
                shipmentToUpdate.Status = viewModel.Shipment.Status;
                shipmentToUpdate.CarrierId = viewModel.Shipment.CarrierId;
                shipmentToUpdate.DeparturePortId = viewModel.Shipment.DeparturePortId;
                shipmentToUpdate.ArrivalPortId = viewModel.Shipment.ArrivalPortId;
                shipmentToUpdate.EstimatedDepartureUtc = viewModel.Shipment.EstimatedDepartureUtc;
                shipmentToUpdate.EstimatedArrivalUtc = viewModel.Shipment.EstimatedArrivalUtc;
                shipmentToUpdate.FreightCost = viewModel.Shipment.FreightCost;
                shipmentToUpdate.Incoterms = viewModel.Shipment.Incoterms;
                shipmentToUpdate.ModifiedAtUtc = DateTime.UtcNow;

                // Konteynerleri güncelle
                UpdateShipmentContainers(shipmentToUpdate, viewModel.Containers);
                // Dökümanları güncelle
                await UpdateShipmentDocuments(shipmentToUpdate, viewModel.Documents);
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sevkiyat başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShipmentExists(id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            PopulateEditDropdowns(viewModel);
            return View(viewModel);
        }
        // GET: Shipments/Delete/5
        // [Authorize] etiketi class seviyesinde olduğu için tüm roller buraya gelebilir.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var shipment = await _context.Shipments
                .Include(s => s.Firm)
                .Include(s => s.Carrier)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);

            if (shipment == null) return NotFound();

            // --- YETKİ KONTROLÜ BURADA YAPILIYOR ---
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");
            bool isOwner = shipment.CreatedByUserId == currentUser.Id;

            // Sadece Admin VEYA sevkiyatın sahibi olan User silebilir.
            if (!isAdmin && !isOwner)
            {
                return Forbid(); // Yetkin yoksa engelle
            }
            // ------------------------------------

            return View(shipment);
        }

        // POST: Shipments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // --- YETKİ KONTROLÜ BURADA DA YAPILIYOR ---
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = User.IsInRole("Admin");
            bool isManager = User.IsInRole("Manager");
            bool isOwner = shipment.CreatedByUserId == currentUser.Id;

            // Sadece Admin VEYA sevkiyatın sahibi olan User silebilir.
            if (!isAdmin && !isManager && !isOwner)
            {
                return Forbid();
            }
            // ------------------------------------
            _context.Shipments.Remove(shipment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sevkiyat başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }


        // --- YARDIMCI METOTLAR ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,User")]
        public async Task<IActionResult> AddDocument(
        int shipmentId,
       [FromForm] DocumentType documentType,
       [FromForm] IFormFile documentFile)
        {
            try
            {
                // Yetki kontrolü
                var shipment = await _context.Shipments.FindAsync(shipmentId);

                if (shipment == null)
                    return NotFound("Sevkiyat bulunamadı");

                var currentUser = await _userManager.GetUserAsync(User);

                if (!User.IsInRole("Admin") && shipment.FirmId != currentUser.FirmId)
                    return Forbid();

                if (documentFile == null || documentFile.Length == 0)
                    return BadRequest("Lütfen bir dosya seçin");

                // Dosya yükleme işlemleri
                var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents");
                Directory.CreateDirectory(uploadPath);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + WebUtility.HtmlEncode(documentFile.FileName);
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }
                var document = new Document
                {
                    ShipmentId = shipmentId,
                    DocumentType = documentType,
                    FilePath = "/documents/" + uniqueFileName,
                    FileName = documentFile.FileName,
                    FileSizeInBytes = documentFile.Length,
                    MimeType = documentFile.ContentType,
                    UploadDateUtc = DateTime.UtcNow,
                    UploadedByUserId = _userManager.GetUserId(User)
                };
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                // AJAX isteği mi kontrolü
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Döküman başarıyla eklendi",
                        documentId = document.DocumentId,
                        fileName = document.FileName
                    });
                }
                TempData["SuccessMessage"] = "Döküman başarıyla eklendi!";
                return RedirectToAction("Edit", new { id = shipmentId });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        error = ex.Message
                    });
                }
                TempData["ErrorMessage"] = $"Hata oluştu: {ex.Message}";
                return RedirectToAction("Edit", new { id = shipmentId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,User")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var document = await _context.Documents.Include(d => d.Shipment).FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (document == null) return NotFound();
            // Yetki kontrolü
            var currentUser = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && document.Shipment.FirmId != currentUser.FirmId) return Forbid();
            var shipmentId = document.ShipmentId;
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Döküman başarıyla silindi!"; 
            // İşlem bittikten sonra, aynı sevkiyatın DÜZENLEME sayfasına geri dön
            return RedirectToAction("Edit", new { id = shipmentId });
        }
        private void PopulateCreateDropdowns(ShipmentCreateViewModel viewModel)

        { 
            // Admin ise Firma listesini doldur, değilse zaten gizli olacak
            if (User.IsInRole("Admin"))
            {
                viewModel.FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive), "FirmId", "FirmName");
            }
            viewModel.CarrierOptions = new SelectList(_context.Carriers.Where(c => c.IsActive), "CarrierId", "CarrierName");
            viewModel.PortOptions = new SelectList(_context.Ports, "PortId", "PortName");
        }
        private void PopulateEditDropdowns(ShipmentEditViewModel viewModel)
        {
            if (User.IsInRole("Admin"))
            {
                viewModel.FirmOptions = new SelectList(_context.Firms.Where(f => f.IsActive), "FirmId", "FirmName", viewModel.Shipment?.FirmId);
            }
            viewModel.CarrierOptions = new SelectList(_context.Carriers.Where(c => c.IsActive), "CarrierId", "CarrierName", viewModel.Shipment?.CarrierId);
            viewModel.DeparturePortOptions = new SelectList(_context.Ports, "PortId", "PortName", viewModel.Shipment?.DeparturePortId);
            viewModel.ArrivalPortOptions = new SelectList(_context.Ports, "PortId", "PortName", viewModel.Shipment?.ArrivalPortId);
        }
        // Konteynerleri güncellemek için YENİ ve AKILLI yardımcı metot
        private void UpdateShipmentContainers(Shipment shipmentToUpdate, ICollection<Container> containersFromForm)
        {
            // Formdan gelmeyen (yani silinmiş olan) konteynerleri veritabanından sil
            var containersToDelete = shipmentToUpdate.Containers
          .Where(c_db => !containersFromForm.Any(c_form => c_form.ContainerId == c_db.ContainerId))
          .ToList();
            _context.Containers.RemoveRange(containersToDelete);
            // Formdan gelenleri işle
            foreach (var formContainer in containersFromForm)
            {
                if (string.IsNullOrEmpty(formContainer.ContainerNumber)) continue;
                // Veritabanında bu ID ile bir konteyner var mı diye bak
                var dbContainer = shipmentToUpdate.Containers.FirstOrDefault(c => c.ContainerId == formContainer.ContainerId);
                if (dbContainer != null) // Eğer varsa, bu MEVCUT bir konteynerdir -> GÜNCELLE
                {
                    dbContainer.ContainerNumber = formContainer.ContainerNumber;
                    dbContainer.ContainerType = formContainer.ContainerType;
                    dbContainer.SealNumber = formContainer.SealNumber;
                    dbContainer.GrossWeightKg = formContainer.GrossWeightKg;
                    dbContainer.PackageQuantity = formContainer.PackageQuantity;
                }
                else // Eğer yoksa ve ID'si 0 ise, bu YENİ bir konteynerdir -> EKLE
                {
                    if (formContainer.ContainerId == 0)
                    {
                        shipmentToUpdate.Containers.Add(formContainer);
                    }
                }
            }
        }
        private async Task UpdateShipmentDocuments(Shipment shipmentToUpdate, List<DocumentEditViewModel> documentViewModels)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Formdan gelen döküman ID'leri
            var viewModelIds = documentViewModels.Where(d => d.DocumentId != 0).Select(d => d.DocumentId).ToHashSet();

            // Veritabanındaki mevcut döküman ID'leri
            var dbDocumentIds = shipmentToUpdate.Documents.Select(d => d.DocumentId).ToHashSet();

            // 1. Silinecek Dökümanları Bul (Veritabanında var ama formda yok)
            var documentsToDelete = shipmentToUpdate.Documents.Where(d => !viewModelIds.Contains(d.DocumentId)).ToList();
            foreach (var doc in documentsToDelete)
            {
                DeleteDocumentFile(doc.FilePath);
                _context.Documents.Remove(doc);
            }

            // 2. Eklenecek ve Güncellenecek Dökümanları İşle
            foreach (var docVm in documentViewModels)
            {
                // YENİ DÖKÜMAN EKLEME (ID'si 0 ise yenidir)
                if (docVm.DocumentId == 0 && docVm.NewDocumentFile != null && docVm.NewDocumentFile.Length > 0)
                {
                    var newDocument = await SaveDocumentFile(docVm.NewDocumentFile, shipmentToUpdate.ShipmentId, docVm.DocumentType);
                    newDocument.UploadedByUserId = currentUser.Id;
                    _context.Documents.Add(newDocument);
                }
                // MEVCUT DÖKÜMANI GÜNCELLEME
                else if (docVm.DocumentId > 0)
                {
                    var docToUpdate = shipmentToUpdate.Documents.FirstOrDefault(d => d.DocumentId == docVm.DocumentId);
                    if (docToUpdate != null)
                    {
                        docToUpdate.DocumentType = docVm.DocumentType;

                        if (docVm.NewDocumentFile != null && docVm.NewDocumentFile.Length > 0)
                        {
                            DeleteDocumentFile(docToUpdate.FilePath);
                            var updatedDocument = await SaveDocumentFile(docVm.NewDocumentFile, shipmentToUpdate.ShipmentId, docVm.DocumentType);
                            docToUpdate.FileName = updatedDocument.FileName;
                            docToUpdate.FilePath = updatedDocument.FilePath;
                            docToUpdate.UploadedByUserId = currentUser.Id;
                        }
                    }
                }
            }
        }

        public IActionResult DownloadDocument(int documentId)
        {
            try
            {
                // 1. Dökümanı veritabanından bul
                var document = _context.Documents
            .FirstOrDefault(d => d.DocumentId == documentId);
                if (document == null)
                {
                    return NotFound("Döküman bulunamadı.");
                }
                // 2. Dosya yolunu oluştur
                var filePath = Path.Combine(Directory.GetCurrentDirectory(),
                       "wwwroot",

                       "uploads",  // Klasör adınıza göre ayarlayoruz
                                          document.FilePath);
                // 3. Dosya var mı kontrol et
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Dosya bulunamadı: {filePath}");
                }
                // 4. Dosya türünü belirle
                var contentType = GetContentType(filePath);

                // 5. Dosyayı indir
                return PhysicalFile(filePath, contentType, document.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Hata oluştu: {ex.Message}");
            }
        }

        private string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        private async Task<Document> SaveDocumentFile(IFormFile file, int shipmentId, DocumentType documentType)
        {
            // Güvenlik kontrolleri (uzantı, boyut, virüs vb.) burada yapılabilir.
            // Şimdilik temel kaydetme işlemini yapıyoruz.

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents");
            Directory.CreateDirectory(uploadPath); // Klasör yoksa oluştur.

            // Dosya adını benzersiz yapalım ki aynı isimli dosyalar birbirini ezmesin.
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadPath, uniqueFileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // Veritabanına kaydedilecek Document nesnesini oluştur ve döndür.
            return new Document
            {
                ShipmentId = shipmentId,
                DocumentType = documentType,
                FileName = Path.GetFileName(file.FileName), // Orijinal dosya adı
                FilePath = "/documents/" + uniqueFileName, // Sunucudaki yolu
                FileSizeInBytes = file.Length,
                MimeType = file.ContentType,
                UploadDateUtc = DateTime.UtcNow,
                UploadedByUserId = _userManager.GetUserId(User),
                VerificationStatus = "Onay Bekliyor"
            };
        }
        private void DeleteDocumentFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            // wwwroot'tan başlayarak tam fiziksel yolu bu
            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/', '\\'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }
        private bool ShipmentExists(int id)
        {
            return _context.Shipments.Any(e => e.ShipmentId == id);
        }

        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                // Arama terimi boşsa, normal listeye yönlendir.
                return RedirectToAction(nameof(Index));
            }

            var searchTerm = term.Trim();

            // Kullanıcının yetkili olduğu sevkiyatlar içinde ara
            var currentUser = await _userManager.GetUserAsync(User);
            IQueryable<Shipment> userShipmentsQuery;
            if (User.IsInRole("Admin")) { userShipmentsQuery = _context.Shipments; }
            else if (User.IsInRole("Manager")) { userShipmentsQuery = _context.Shipments.Where(s => s.FirmId == currentUser.FirmId); }
            else { userShipmentsQuery = _context.Shipments.Where(s => s.CreatedByUserId == currentUser.Id); }


            // Girilen terimle tam olarak eşleşen sevkiyatları bul
            var matchingShipments = await userShipmentsQuery
                                            .Where(s => s.ReferenceNumber.ToUpper() == searchTerm.ToUpper())
                                            .ToListAsync();

            if (matchingShipments.Count == 1)
            {
                // Eğer tam olarak 1 sonuç bulunduysa, doğrudan o sevkiyatın detay sayfasına git.
                var shipment = matchingShipments.First();
                return RedirectToAction("Details", "Shipments", new { id = shipment.ShipmentId });
            }
            else
            {
                // Eğer 0 veya 1'den fazla sonuç varsa, arama terimini Index sayfasına filtre olarak gönder.
                // Bu, "buna benzer sonuçlar" göstermemizi sağlar.
                return RedirectToAction(nameof(Index), new { searchString = searchTerm });
            }
        }
    }
}
