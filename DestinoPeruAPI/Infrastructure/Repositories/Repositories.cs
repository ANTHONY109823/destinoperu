using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace DestinoPeruAPI.Infrastructure.Repositories;
public class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    protected readonly AppDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    public virtual async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public virtual async Task<T> AddAsync(T entity) { await _dbSet.AddAsync(entity); await _context.SaveChangesAsync(); return entity; }
    public virtual async Task UpdateAsync(T entity) { _dbSet.Update(entity); await _context.SaveChangesAsync(); }
    public virtual async Task DeleteAsync(int id) { var e = await GetByIdAsync(id); if (e != null) { _dbSet.Remove(e); await _context.SaveChangesAsync(); } }
}
public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email) => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    public async Task<bool> ExistsEmailAsync(string email) => await _dbSet.AnyAsync(u => u.Email == email);
}
public class AgencyRepository(AppDbContext context) : Repository<Agency>(context), IAgencyRepository
{
    public async Task<Agency?> GetByUserIdAsync(int userId) => await _dbSet.Include(a => a.User).FirstOrDefaultAsync(a => a.UserId == userId);
    public async Task<Agency?> GetWithToursAsync(int id) => await _dbSet.Include(a => a.Tours).Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
    public async Task<IEnumerable<Agency>> GetPendingAsync() => await _dbSet.Where(a => a.Status == "Pending").Include(a => a.User).ToListAsync();
    public override async Task<IEnumerable<Agency>> GetAllAsync() => await _dbSet.Include(a => a.User).ToListAsync();
}
public class TourRepository(AppDbContext context) : Repository<Tour>(context), ITourRepository
{
    public async Task<IEnumerable<Tour>> GetActiveAsync() => await _dbSet.Where(t => t.IsActive && t.Date > DateTime.UtcNow).Include(t => t.Agency).OrderBy(t => t.Date).ToListAsync();
    public async Task<IEnumerable<Tour>> GetByAgencyAsync(int agencyId) => await _dbSet.Where(t => t.AgencyId == agencyId).Include(t => t.Agency).ToListAsync();
    public async Task<Tour?> GetWithAgencyAsync(int id) => await _dbSet.Include(t => t.Agency).FirstOrDefaultAsync(t => t.Id == id);
    public async Task<IEnumerable<Tour>> SearchAsync(string? location, DateTime? fromDate, decimal? maxPrice)
    {
        var q = _dbSet.Where(t => t.IsActive && t.Date > DateTime.UtcNow).Include(t => t.Agency).AsQueryable();
        if (!string.IsNullOrWhiteSpace(location)) q = q.Where(t => t.Location.Contains(location) || t.Title.Contains(location));
        if (fromDate.HasValue) q = q.Where(t => t.Date >= fromDate.Value);
        if (maxPrice.HasValue) q = q.Where(t => t.Price <= maxPrice.Value);
        return await q.OrderBy(t => t.Date).ToListAsync();
    }
}
public class ReservationRepository(AppDbContext context) : Repository<Reservation>(context), IReservationRepository
{
    public async Task<IEnumerable<Reservation>> GetByUserAsync(int userId) => await _dbSet.Where(r => r.UserId == userId).Include(r => r.Tour).ThenInclude(t => t.Agency).Include(r => r.User).OrderByDescending(r => r.CreatedAt).ToListAsync();
    public async Task<IEnumerable<Reservation>> GetByAgencyAsync(int agencyId) => await _dbSet.Where(r => r.Tour.AgencyId == agencyId).Include(r => r.Tour).Include(r => r.User).OrderByDescending(r => r.CreatedAt).ToListAsync();
    public async Task<Reservation?> GetWithDetailsAsync(int id) => await _dbSet.Include(r => r.Tour).ThenInclude(t => t.Agency).Include(r => r.User).Include(r => r.Payment).FirstOrDefaultAsync(r => r.Id == id);
    public async Task<int> GetTotalReservedAsync(int tourId) => await _dbSet.Where(r => r.TourId == tourId && r.Status != "Cancelled").SumAsync(r => r.Quantity);
}
public class PaymentRepository(AppDbContext context) : Repository<Payment>(context), IPaymentRepository
{
    public async Task<Payment?> GetByReservationAsync(int reservationId) => await _dbSet.FirstOrDefaultAsync(p => p.ReservationId == reservationId);
}