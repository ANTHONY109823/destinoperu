using DestinoPeruAPI.Domain.Entities;
namespace DestinoPeruAPI.Application.Interfaces;
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsEmailAsync(string email);
}
public interface IAgencyRepository : IRepository<Agency>
{
    Task<Agency?> GetByUserIdAsync(int userId);
    Task<Agency?> GetWithToursAsync(int id);
    Task<IEnumerable<Agency>> GetPendingAsync();
}
public interface ITourRepository : IRepository<Tour>
{
    Task<IEnumerable<Tour>> GetActiveAsync();
    Task<IEnumerable<Tour>> GetByAgencyAsync(int agencyId);
    Task<Tour?> GetWithAgencyAsync(int id);
    Task<IEnumerable<Tour>> SearchAsync(string? location, DateTime? fromDate, decimal? maxPrice);
}
public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetByUserAsync(int userId);
    Task<IEnumerable<Reservation>> GetByAgencyAsync(int agencyId);
    Task<Reservation?> GetWithDetailsAsync(int id);
    Task<int> GetTotalReservedAsync(int tourId);
}
public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByReservationAsync(int reservationId);
}
public interface IJwtService
{
    string GenerateToken(int userId, string email, string role, string name);
}