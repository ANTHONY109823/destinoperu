using DestinoPeruAPI.Domain.Entities;

namespace DestinoPeruAPI.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsEmailAsync(string email);
    Task<User> AddAsync(User entity);
    Task UpdateAsync(User entity);
}

public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(int id);
    Task<Partner?> GetByUserIdAsync(int userId);
    Task<Partner?> GetWithToursAsync(int id);
    Task<IEnumerable<Partner>> GetAllAsync();
    Task<Partner> AddAsync(Partner entity);
    Task UpdateAsync(Partner entity);
    Task DeleteAsync(int partnerId);
    Task<PartnerDocument> AddDocumentAsync(PartnerDocument doc);
    Task<PartnerDocument?> GetDocumentAsync(int id);
    Task UpdateDocumentAsync(PartnerDocument doc);
    Task<int> GetStaffCountAsync(int partnerId);
    Task<PartnerStaff?> GetStaffByUserIdAsync(int userId);
    Task<PartnerStaff> AddStaffAsync(PartnerStaff staff);
    Task<IReadOnlyList<PartnerStaff>> GetStaffByPartnerIdAsync(int partnerId);
}

public interface ITourRepository
{
    Task<Tour?> GetByIdAsync(int id);
    Task<Tour?> GetBySlugAsync(string slug);
    Task<Tour> AddAsync(Tour entity);
    Task UpdateAsync(Tour entity);
    Task DeleteAsync(int id);
    Task<bool> HasReservationsAsync(int tourId);
    Task<bool> TryReserveCapacityAsync(int tourId, int quantity);
}

public interface IReservationRepository
{
    Task<Reservation?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Reservation>> GetByUserAsync(int userId);
    Task<Reservation> AddAsync(Reservation entity);
    Task UpdateAsync(Reservation entity);
    Task AddPassengersAsync(IEnumerable<PassengerManifest> passengers);
    Task<IEnumerable<Reservation>> GetByPartnerAsync(int partnerId);
}

public interface ILoyaltyRepository
{
    Task<LoyaltyAccount?> GetByUserAsync(int userId);
    Task AddPointsAsync(int userId, int points);
}

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id);
    Task<Payment?> GetByReservationAsync(int reservationId);
    Task<Payment> AddAsync(Payment entity);
    Task UpdateAsync(Payment entity);
}

public interface IJwtService
{
    string GenerateToken(int userId, string email, string role, string name, int? partnerId = null, bool impersonating = false);
}
