using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DestinoPeruAPI.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id) => await context.Users.FindAsync(id);
    public async Task<User?> GetByEmailAsync(string email) => await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    public async Task<bool> ExistsEmailAsync(string email) => await context.Users.AnyAsync(u => u.Email == email);
    public async Task<User> AddAsync(User entity) { await context.Users.AddAsync(entity); await context.SaveChangesAsync(); return entity; }
    public async Task UpdateAsync(User entity) { context.Users.Update(entity); await context.SaveChangesAsync(); }
}

public class PartnerRepository(AppDbContext context) : IPartnerRepository
{
    public async Task<Partner?> GetByIdAsync(int id) => await context.Partners.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
    public async Task<Partner?> GetByUserIdAsync(int userId) => await context.Partners.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == userId);
    public async Task<Partner?> GetWithToursAsync(int id) => await context.Partners.Include(p => p.Tours).Include(p => p.User).Include(p => p.Documents).FirstOrDefaultAsync(p => p.Id == id);
    public async Task<IEnumerable<Partner>> GetAllAsync() => await context.Partners.Include(p => p.User).ToListAsync();
    public async Task<Partner> AddAsync(Partner entity) { await context.Partners.AddAsync(entity); await context.SaveChangesAsync(); return entity; }
    public async Task UpdateAsync(Partner entity) { context.Partners.Update(entity); await context.SaveChangesAsync(); }
    public async Task<PartnerDocument> AddDocumentAsync(PartnerDocument doc) { await context.PartnerDocuments.AddAsync(doc); await context.SaveChangesAsync(); return doc; }
    public async Task<PartnerDocument?> GetDocumentAsync(int id) => await context.PartnerDocuments.FindAsync(id);
    public async Task UpdateDocumentAsync(PartnerDocument doc) { context.PartnerDocuments.Update(doc); await context.SaveChangesAsync(); }

    public async Task<int> GetStaffCountAsync(int partnerId) =>
        await context.PartnerStaff.CountAsync(s => s.PartnerId == partnerId);

    public async Task<PartnerStaff?> GetStaffByUserIdAsync(int userId) =>
        await context.PartnerStaff.Include(s => s.Partner).FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<PartnerStaff> AddStaffAsync(PartnerStaff staff)
    {
        await context.PartnerStaff.AddAsync(staff);
        await context.SaveChangesAsync();
        return staff;
    }
}

public class TourRepository(AppDbContext context) : ITourRepository
{
    public async Task<Tour?> GetByIdAsync(int id) => await context.Tours.FindAsync(id);
    public async Task<Tour?> GetBySlugAsync(string slug) => await context.Tours.FirstOrDefaultAsync(t => t.Slug == slug);
    public async Task<Tour> AddAsync(Tour entity) { await context.Tours.AddAsync(entity); await context.SaveChangesAsync(); return entity; }
    public async Task UpdateAsync(Tour entity) { context.Tours.Update(entity); await context.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var e = await GetByIdAsync(id); if (e != null) { context.Tours.Remove(e); await context.SaveChangesAsync(); } }

    public async Task<bool> TryReserveCapacityAsync(int tourId, int quantity)
    {
        var rows = await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "Tours"
            SET "AvailableCapacity" = "AvailableCapacity" - {quantity}
            WHERE "Id" = {tourId} AND "AvailableCapacity" >= {quantity} AND "IsActive" = true
            """);
        return rows > 0;
    }
}

public class ReservationRepository(AppDbContext context) : IReservationRepository
{
    public async Task<IEnumerable<Reservation>> GetByUserAsync(int userId) => await context.Reservations
        .Where(r => r.UserId == userId)
        .Include(r => r.Tour).ThenInclude(t => t.Partner)
        .Include(r => r.User)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    public async Task<Reservation?> GetWithDetailsAsync(int id) => await context.Reservations
        .Include(r => r.Tour).ThenInclude(t => t.Partner)
        .Include(r => r.User).Include(r => r.Payment).Include(r => r.Passengers)
        .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Reservation> AddAsync(Reservation entity)
    {
        await context.Reservations.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Reservation entity) { context.Reservations.Update(entity); await context.SaveChangesAsync(); }

    public async Task AddPassengersAsync(IEnumerable<PassengerManifest> passengers)
    {
        await context.PassengerManifests.AddRangeAsync(passengers);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Reservation>> GetByPartnerAsync(int partnerId) => await context.Reservations
        .Where(r => r.Tour.PartnerId == partnerId)
        .Include(r => r.Tour).Include(r => r.User).Include(r => r.Passengers)
        .OrderByDescending(r => r.CreatedAt).ToListAsync();
}

public class LoyaltyRepository(AppDbContext context) : ILoyaltyRepository
{
    public async Task<LoyaltyAccount?> GetByUserAsync(int userId) => await context.LoyaltyAccounts.FirstOrDefaultAsync(l => l.UserId == userId);

    public async Task AddPointsAsync(int userId, int points)
    {
        var account = await GetByUserAsync(userId);
        if (account == null)
        {
            account = new LoyaltyAccount { UserId = userId, Points = points, LifetimePoints = points };
            await context.LoyaltyAccounts.AddAsync(account);
        }
        else
        {
            account.Points += points;
            account.LifetimePoints += points;
            account.UpdatedAt = DateTime.UtcNow;
            context.LoyaltyAccounts.Update(account);
        }
        await context.SaveChangesAsync();
    }
}

public class PaymentRepository(AppDbContext context) : IPaymentRepository
{
    public async Task<Payment?> GetByIdAsync(int id) => await context.Payments.FindAsync(id);
    public async Task<Payment?> GetByReservationAsync(int reservationId) => await context.Payments.FirstOrDefaultAsync(p => p.ReservationId == reservationId);
    public async Task<Payment> AddAsync(Payment entity) { await context.Payments.AddAsync(entity); await context.SaveChangesAsync(); return entity; }
    public async Task UpdateAsync(Payment entity) { context.Payments.Update(entity); await context.SaveChangesAsync(); }
}
