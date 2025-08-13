using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Models;

namespace NaviGate.Web.Data
{
    public class AppDbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

                // Rolleri oluştur (Bu kısım aynı kalıyor)
                string[] roleNames = { "Admin", "Manager", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                }

                // --- DEĞİŞEN MANTIK BURADA BAŞLIYOR ---

                // Sistemde "Admin" rolüne sahip herhangi bir kullanıcı var mı diye kontrol et.
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count == 0)
                {
                    // Eğer hiç admin yoksa, varsayılan firmayı oluştur.
                    string defaultFirmName = "Sistem Yönetimi";
                    var firm = await context.Firms.FirstOrDefaultAsync(f => f.FirmName == defaultFirmName);
                    if (firm == null)
                    {
                        firm = new Firm() { FirmName = defaultFirmName, FirmType = FirmTypeEnum.Dahili, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
                        await context.Firms.AddAsync(firm);
                        await context.SaveChangesAsync();
                    }

                    // Ve İLK admin kullanıcısını oluştur.
                    var newAdminUser = new User()
                    {
                        FirstName = "Admin",
                        LastName = "Kullanici",
                        UserName = "adminuser",
                        Email = "admin@navigate.com",
                        EmailConfirmed = true,
                        FirmId = firm.FirmId
                    };

                    var result = await userManager.CreateAsync(newAdminUser, "Admin44."); // Şifreyi de güncelleyelim
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdminUser, "Admin");
                    }
                }
            }
        }
    }
}
