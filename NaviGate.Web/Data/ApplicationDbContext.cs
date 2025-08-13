using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NaviGate.Web.Models; 

namespace NaviGate.Web.Data 
{
    public class ApplicationDbContext : IdentityDbContext<User> // IdentityUser yerine kendi User sınıfımızı veriyoruz
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Kendi modellerimiz için DbSet'leri buraya ekliyoruz
        public DbSet<Firm> Firms { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Container> Containers { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<AiAlert> AiAlerts { get; set; }
        public DbSet<Carrier> Carriers { get; set; }
        public DbSet<Port> Ports { get; set; }
        public DbSet<ShipmentTracking> ShipmentTrackings { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity için bu satır kalmalı.

            // --- ÖNCEKİ ÇÖZÜMÜMÜZ (Bu kalsın) ---
            builder.Entity<Document>()
                .HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Shipment>()
                .HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- YENİ ÇÖZÜMÜMÜZ (Bu iki bloğu ekle) ---
            // Bir liman silinirse, o limanı KALKIŞ olarak kullanan sevkiyatların silinmesini engelle.
            builder.Entity<Shipment>()
                .HasOne(s => s.DeparturePort)
                .WithMany() // Port tarafında bir ICollection<Shipment> olmadığı için boş.
                .HasForeignKey(s => s.DeparturePortId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir liman silinirse, o limanı VARIŞ olarak kullanan sevkiyatların silinmesini engelle.
            builder.Entity<Shipment>()
                .HasOne(s => s.ArrivalPort)
                .WithMany()
                .HasForeignKey(s => s.ArrivalPortId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Container>()
                .HasOne(c => c.Shipment) // Container'ın bir Shipment'ı vardır
                .WithMany(s => s.Containers) // Shipment'ın birden çok Container'ı vardır
                .HasForeignKey(c => c.ShipmentId); // Foreign Key ShipmentId'dir
        }
    }
}
