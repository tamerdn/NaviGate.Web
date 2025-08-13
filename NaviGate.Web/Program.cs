using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Data;
using NaviGate.Web.Models;
using NaviGate.Web.Services;
using System.Globalization;

namespace NaviGate.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // 1. Veritabanı Bağlantısı
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // 2. Kimlik Doğrulama (Identity) Servisleri - EN KRİTİK KISIM
            // Bu blok, UserManager, SignInManager, RoleManager gibi tüm servisleri doğru şekilde kaydeder.
            builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // 3. MVC Servisleri
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IVirusScannerService, FakeVirusScannerService>();
            builder.Services.AddScoped<IFirmRepository, FirmRepository>();
            builder.Services.AddHostedService<FakeTrackingGeneratorService>();
            builder.Services.AddHostedService<AiAlertGeneratorService>();

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("tr-TR")
                    // İleride başka dilleri desteklemek isterseniz buraya ekleyebilirsiniz.
                    // new CultureInfo("en-US")
    };

                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("tr-TR");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            var app = builder.Build();

            // 4. Türkçe Kültür Ayarları
            var supportedCultures = new[] { new CultureInfo("tr-TR") };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("tr-TR"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            // 5. Başlangıç Verilerini Oluşturma
            await AppDbInitializer.SeedRolesAndAdminAsync(app);


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRequestLocalization();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}