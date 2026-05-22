using DestinoPeruAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace DestinoPeruAPI.Infrastructure.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(e => {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Cliente");
        });
        modelBuilder.Entity<Agency>(e => {
            e.HasIndex(a => a.RUC).IsUnique();
            e.Property(a => a.Status).HasDefaultValue("Pending");
            e.HasOne(a => a.User).WithOne(u => u.Agency)
             .HasForeignKey<Agency>(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Tour>(e => {
            e.Property(t => t.Price).HasPrecision(10, 2);
            e.HasOne(t => t.Agency).WithMany(a => a.Tours)
             .HasForeignKey(t => t.AgencyId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Reservation>(e => {
            e.Property(r => r.Total).HasPrecision(10, 2);
            e.Property(r => r.Commission).HasPrecision(10, 2);
            e.Property(r => r.Status).HasDefaultValue("Pending");
            e.HasOne(r => r.User).WithMany(u => u.Reservations)
             .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Tour).WithMany(t => t.Reservations)
             .HasForeignKey(r => r.TourId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Payment>(e => {
            e.Property(p => p.Amount).HasPrecision(10, 2);
            e.HasOne(p => p.Reservation).WithOne(r => r.Payment)
             .HasForeignKey<Payment>(p => p.ReservationId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}