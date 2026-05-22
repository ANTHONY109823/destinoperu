using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;

namespace DestinoPeruAPI.Application.Services;

public class AuthService(IUserRepository userRepository, IJwtService jwtService)
{
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await userRepository.ExistsEmailAsync(request.Email))
            return new ApiResponse<AuthResponse>(false, "El email ya está registrado.", null);
        var user = new User { Name = request.Name, Email = request.Email.ToLower().Trim(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12), Role = request.Role };
        await userRepository.AddAsync(user);
        var token = jwtService.GenerateToken(user.Id, user.Email, user.Role, user.Name);
        return new ApiResponse<AuthResponse>(true, "Registro exitoso.", new AuthResponse(token, user.Name, user.Email, user.Role, user.Id));
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLower().Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new ApiResponse<AuthResponse>(false, "Credenciales incorrectas.", null);
        var token = jwtService.GenerateToken(user.Id, user.Email, user.Role, user.Name);
        return new ApiResponse<AuthResponse>(true, "Login exitoso.", new AuthResponse(token, user.Name, user.Email, user.Role, user.Id));
    }
}

public class TourService(ITourRepository tourRepository, IAgencyRepository agencyRepository)
{
    public async Task<IEnumerable<TourDto>> GetAllActiveAsync() => (await tourRepository.GetActiveAsync()).Select(MapToDto);
    public async Task<IEnumerable<TourDto>> SearchAsync(string? location, DateTime? fromDate, decimal? maxPrice) => (await tourRepository.SearchAsync(location, fromDate, maxPrice)).Select(MapToDto);
    public async Task<ApiResponse<TourDto>> GetByIdAsync(int id) { var t = await tourRepository.GetWithAgencyAsync(id); return t == null ? new ApiResponse<TourDto>(false, "Tour no encontrado.", null) : new ApiResponse<TourDto>(true, null, MapToDto(t)); }
    public async Task<IEnumerable<TourDto>> GetByAgencyAsync(int agencyId) => (await tourRepository.GetByAgencyAsync(agencyId)).Select(MapToDto);

    public async Task<ApiResponse<TourDto>> CreateAsync(CreateTourRequest request, int userId)
    {
        var agency = await agencyRepository.GetByUserIdAsync(userId);
        if (agency == null) return new ApiResponse<TourDto>(false, "No tienes una agencia registrada.", null);
        if (agency.Status != "Approved") return new ApiResponse<TourDto>(false, "Tu agencia aún no está aprobada.", null);
        var tour = new Tour { AgencyId = agency.Id, Title = request.Title, Description = request.Description, Price = request.Price, Location = request.Location, Date = request.Date, Capacity = request.Capacity, ImageUrl = request.ImageUrl };
        await tourRepository.AddAsync(tour);
        var created = await tourRepository.GetWithAgencyAsync(tour.Id);
        return new ApiResponse<TourDto>(true, "Tour creado.", MapToDto(created!));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId, string role)
    {
        var tour = await tourRepository.GetByIdAsync(id);
        if (tour == null) return new ApiResponse<bool>(false, "Tour no encontrado.", false);
        if (role != "Admin") { var agency = await agencyRepository.GetByUserIdAsync(userId); if (agency == null || tour.AgencyId != agency.Id) return new ApiResponse<bool>(false, "Sin permiso.", false); }
        await tourRepository.DeleteAsync(id);
        return new ApiResponse<bool>(true, "Tour eliminado.", true);
    }

    private static TourDto MapToDto(Tour t) => new(t.Id, t.AgencyId, t.Agency?.Name ?? "", t.Title, t.Description, t.Price, t.Location, t.Date, t.Capacity, t.ImageUrl, t.IsActive, t.CreatedAt);
}

public class AgencyService(IAgencyRepository agencyRepository, IUserRepository userRepository)
{
    public async Task<IEnumerable<AgencyDto>> GetAllAsync() => (await agencyRepository.GetAllAsync()).Select(MapToDto);

    public async Task<ApiResponse<AgencyDto>> CreateAsync(CreateAgencyRequest request, int userId)
    {
        var existing = await agencyRepository.GetByUserIdAsync(userId);
        if (existing != null) return new ApiResponse<AgencyDto>(false, "Ya tienes una agencia.", null);
        var agency = new Agency { UserId = userId, Name = request.Name, RUC = request.RUC };
        await agencyRepository.AddAsync(agency);
        var user = await userRepository.GetByIdAsync(userId);
        user!.Role = "Agencia"; await userRepository.UpdateAsync(user);
        return new ApiResponse<AgencyDto>(true, "Agencia registrada. Pendiente de aprobación.", MapToDto(agency));
    }

    public async Task<ApiResponse<AgencyDto>> ApproveAsync(int id)
    {
        var agency = await agencyRepository.GetByIdAsync(id);
        if (agency == null) return new ApiResponse<AgencyDto>(false, "Agencia no encontrada.", null);
        agency.Status = "Approved"; await agencyRepository.UpdateAsync(agency);
        return new ApiResponse<AgencyDto>(true, "Agencia aprobada.", MapToDto(agency));
    }

    private static AgencyDto MapToDto(Agency a) => new(a.Id, a.UserId, a.User?.Name ?? "", a.Name, a.RUC, a.Status, a.CreatedAt);
}
