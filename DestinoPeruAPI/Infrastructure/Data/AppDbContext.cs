using DestinoPeruAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DestinoPeruAPI.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<PartnerDocument> PartnerDocuments => Set<PartnerDocument>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<PassengerManifest> PassengerManifests => Set<PassengerManifest>();
    public DbSet<PartnerStaff> PartnerStaff => Set<PartnerStaff>();
    public DbSet<AppMaintenanceRun> AppMaintenanceRuns => Set<AppMaintenanceRun>();
    public DbSet<PopularDestination> PopularDestinations => Set<PopularDestination>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppMaintenanceRun>(e =>
        {
            e.ToTable("AppMaintenanceRuns");
            e.HasKey(r => r.Key);
        });

        modelBuilder.Entity<PopularDestination>(e =>
        {
            e.ToTable("PopularDestinations");
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired();
            e.HasIndex(d => new { d.IsActive, d.DisplayOrder });
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Cliente");
        });

        modelBuilder.Entity<Partner>(e =>
        {
            e.ToTable("Partners");
            e.HasIndex(p => p.RUC).IsUnique();
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Status).HasDefaultValue("Pending");
            e.Property(p => p.VerificationStatus).HasDefaultValue("Pending");
            e.Property(p => p.CommissionRate).HasPrecision(5, 4);
            e.HasOne(p => p.User).WithOne(u => u.Partner)
                .HasForeignKey<Partner>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartnerDocument>(e =>
        {
            e.HasIndex(d => new { d.PartnerId, d.DocumentType });
            e.HasOne(d => d.Partner).WithMany(p => p.Documents)
                .HasForeignKey(d => d.PartnerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tour>(e =>
        {
            e.Property(t => t.Price).HasPrecision(10, 2);
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasIndex(t => new { t.Department, t.IsActive, t.Date });
            e.HasIndex(t => t.AdventureType);
            e.Property(t => t.RowVersion).IsRowVersion();
            e.HasOne(t => t.Partner).WithMany(p => p.Tours)
                .HasForeignKey(t => t.PartnerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoyaltyAccount>(e =>
        {
            e.HasIndex(l => l.UserId).IsUnique();
            e.HasOne(l => l.User).WithOne(u => u.LoyaltyAccount)
                .HasForeignKey<LoyaltyAccount>(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(r => r.Total).HasPrecision(10, 2);
            e.Property(r => r.Commission).HasPrecision(10, 2);
            e.Property(r => r.Status).HasDefaultValue("Pending");
            e.HasOne(r => r.User).WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Tour).WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TourId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PassengerManifest>(e =>
        {
            e.HasOne(p => p.Reservation).WithMany(r => r.Passengers)
                .HasForeignKey(p => p.ReservationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(10, 2);
            e.HasOne(p => p.Reservation).WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.ReservationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartnerStaff>(e =>
        {
            e.HasIndex(s => new { s.PartnerId, s.UserId }).IsUnique();
            e.HasOne(s => s.Partner).WithMany(p => p.Staff)
                .HasForeignKey(s => s.PartnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.User).WithMany()
                .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
