using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.Services;

namespace NaviGate.Web.Services;

public class FirmRepository : IFirmRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public FirmRepository(
        ApplicationDbContext context,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IdentityResult> DeleteFirmWithDependenciesAsync(int firmId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var firm = await _context.Firms
                .Include(f => f.Users)
                .Include(f => f.Shipments)
                    .ThenInclude(s => s.Containers)
                .Include(f => f.Shipments)
                    .ThenInclude(s => s.Documents)
                .FirstOrDefaultAsync(f => f.FirmId == firmId);

            if (firm == null)
                return IdentityResult.Failed(new IdentityError { Description = "Firma bulunamadı" });

            // Fiziksel dosyaları sil
            foreach (var doc in firm.Shipments.SelectMany(s => s.Documents))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, doc.FilePath.TrimStart('/'));
                if (File.Exists(filePath)) File.Delete(filePath);
            }

            // Veritabanı kayıtlarını sil
            _context.Containers.RemoveRange(firm.Shipments.SelectMany(s => s.Containers));
            _context.Documents.RemoveRange(firm.Shipments.SelectMany(s => s.Documents));
            _context.Shipments.RemoveRange(firm.Shipments);
            _context.Users.RemoveRange(firm.Users);
            _context.Firms.Remove(firm);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }
}