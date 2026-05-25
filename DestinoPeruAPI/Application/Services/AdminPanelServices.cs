using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using DestinoPeruAPI.Infrastructure.Data;

namespace DestinoPeruAPI.Application.Services;

public class SuperAdminService(
    IPartnerRepository partnerRepository,
    IPartnerQueryRepository partnerQuery,
    IUserRepository userRepository,
    IJwtService jwtService,
    AppDbContext appDb)
{
    public async Task<SuperAdminMetricsDto> GetMetricsAsync()
    {
        try { return await partnerQuery.GetSuperAdminMetricsAsync(); }
        catch
        {
            var all = (await partnerRepository.GetAllAsync()).ToList();
            return new SuperAdminMetricsDto(
                0, all.Count, all.Count(p => p.Status == "Pending"), 0, 0, 0, 0, 0);
        }
    }

    public async Task<IReadOnlyList<PartnerListItemDto>> GetPartnersAsync()
    {
        try { return await partnerQuery.GetAllPartnersListAsync(); }
        catch { return await GetPartnersFromEfAsync(); }
    }

    private async Task<IReadOnlyList<PartnerListItemDto>> GetPartnersFromEfAsync()
    {
        var list = new List<PartnerListItemDto>();
        foreach (var p in await partnerRepository.GetAllAsync())
        {
            var u = p.User;
            list.Add(new PartnerListItemDto(
                p.Id, p.Name, p.RUC, p.Status, p.OperatingDepartment ?? "",
                u?.Email ?? "", u?.Name ?? "", u?.Id ?? 0,
                await partnerRepository.GetStaffCountAsync(p.Id), 0));
        }
        return list;
    }

    public async Task<ApiResponse<PartnerDto>> CreateAgencyAsync(CreateAgencyRequest request)
    {
        var email = request.AdminEmail.ToLower().Trim();
        if (await userRepository.ExistsEmailAsync(email))
            return new ApiResponse<PartnerDto>(false, "El email del administrador ya está registrado.", null);
        if (await appDb.Partners.AnyAsync(p => p.RUC == request.RUC))
            return new ApiResponse<PartnerDto>(false, "El RUC ya está registrado.", null);

        var admin = new User
        {
            Name = request.AdminName.Trim(),
            Email = email,
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword, 12),
            CreatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(admin);

        var partner = new Partner
        {
            UserId = admin.Id,
            Name = request.AgencyName.Trim(),
            RUC = request.RUC.Trim(),
            PartnerType = PartnerType.Agencia,
            Status = "Approved",
            VerificationStatus = "Verified",
            OperatingDepartment = request.OperatingDepartment.Trim(),
            LogoUrl = request.LogoUrl,
            ContactEmail = request.ContactEmail ?? email,
            ContactPhone = request.ContactPhone,
            CommissionRate = 0.10m,
            CreatedAt = DateTime.UtcNow
        };
        await partnerRepository.AddAsync(partner);
        return new ApiResponse<PartnerDto>(true, "Agencia creada con administrador inicial.", MapPartner(partner, admin.Name));
    }

    public async Task<ApiResponse<bool>> SuspendPartnerAsync(int partnerId, bool suspend)
    {
        var partner = await partnerRepository.GetByIdAsync(partnerId);
        if (partner == null) return new ApiResponse<bool>(false, "Agencia no encontrada.", false);
        partner.Status = suspend ? "Suspended" : "Approved";
        await partnerRepository.UpdateAsync(partner);
        return new ApiResponse<bool>(true, suspend ? "Agencia suspendida." : "Agencia reactivada.", true);
    }

    public async Task<ApiResponse<AuthResponse>> ImpersonateAsync(int targetUserId, int superAdminId)
    {
        var target = await userRepository.GetByIdAsync(targetUserId);
        if (target == null) return new ApiResponse<AuthResponse>(false, "Usuario no encontrado.", null);

        int? partnerId = null;
        if (target.Role is "Admin" or "Agencia")
        {
            var p = await partnerRepository.GetByUserIdAsync(target.Id);
            partnerId = p?.Id;
        }
        else if (target.Role == "Vendedor")
        {
            var staff = await partnerRepository.GetStaffByUserIdAsync(target.Id);
            partnerId = staff?.PartnerId;
        }

        var token = jwtService.GenerateToken(target.Id, target.Email, target.Role, target.Name, partnerId, impersonating: true);
        return new ApiResponse<AuthResponse>(true, "Modo soporte (suplantación) activado.",
            new AuthResponse(token, target.Name, target.Email, target.Role, target.Id, partnerId, true));
    }

    private static PartnerDto MapPartner(Partner p, string userName) => new(
        p.Id, p.UserId, userName, p.Name, p.RUC, p.PartnerType, p.Status, p.VerificationStatus,
        p.CommissionRate, p.CreatedAt, 0, p.OperatingDepartment, p.LogoUrl, p.ContactEmail, p.ContactPhone, 0);
}

public class AgencyAdminService(
    IPartnerRepository partnerRepository,
    IPartnerQueryRepository partnerQuery,
    IUserRepository userRepository,
    IReservationRepository reservationRepository,
    ITourRepository tourRepository,
    TourService tourService)
{
    public async Task<int?> ResolvePartnerIdAsync(int userId, string role)
    {
        if (role == "SuperAdmin") return null;
        if (role is "Admin" or "Agencia")
        {
            var p = await partnerRepository.GetByUserIdAsync(userId);
            return p?.Id;
        }
        if (role == "Vendedor")
        {
            var staff = await partnerRepository.GetStaffByUserIdAsync(userId);
            return staff?.PartnerId;
        }
        return null;
    }

    public async Task<ApiResponse<AgencyDashboardDto>> GetDashboardAsync(int partnerId)
    {
        try
        {
            var d = await partnerQuery.GetAgencyDashboardAsync(partnerId);
            if (d is not null) return new ApiResponse<AgencyDashboardDto>(true, null, d);
        }
        catch { /* fallback EF */ }

        var partner = await partnerRepository.GetWithToursAsync(partnerId);
        if (partner is null) return new ApiResponse<AgencyDashboardDto>(false, "Agencia no encontrada.", null);
        var tourCount = partner.Tours.Count(t => t.IsActive);
        return new ApiResponse<AgencyDashboardDto>(true, null, new AgencyDashboardDto(
            partner.Id, partner.Name, tourCount, 0, 0, 0, 0, []));
    }

    public async Task<ApiResponse<AgencyProfileDto>> GetProfileAsync(int partnerId)
    {
        var p = await partnerRepository.GetByIdAsync(partnerId);
        if (p is null) return new ApiResponse<AgencyProfileDto>(false, "Agencia no encontrada.", null);
        return new ApiResponse<AgencyProfileDto>(true, null, new AgencyProfileDto(
            p.Id, p.Name, p.RUC, p.LogoUrl, p.OperatingDepartment,
            p.ContactEmail, p.ContactPhone, p.Status, p.CommissionRate));
    }

    public async Task<ApiResponse<List<ReservationDto>>> GetReservationsAsync(int partnerId) =>
        new(true, null, (await reservationRepository.GetByPartnerAsync(partnerId))
            .Select(MapReservation).ToList());

    public async Task<ApiResponse<bool>> SetReservationStatusAsync(int reservationId, int partnerId, string status)
    {
        var list = await reservationRepository.GetByPartnerAsync(partnerId);
        var r = list.FirstOrDefault(x => x.Id == reservationId);
        if (r == null) return new ApiResponse<bool>(false, "Reserva no encontrada.", false);
        r.Status = status;
        await reservationRepository.UpdateAsync(r);
        return new ApiResponse<bool>(true, $"Reserva marcada como {status}.", true);
    }

    public async Task<ApiResponse<VendorSalesDto>> CreateVendorAsync(int partnerId, CreateVendorRequest request, string role)
    {
        if (role != "Admin" && role != "SuperAdmin")
            return new ApiResponse<VendorSalesDto>(false, "Solo el administrador de agencia puede crear vendedores.", null);

        var count = await partnerRepository.GetStaffCountAsync(partnerId);
        if (count >= 5)
            return new ApiResponse<VendorSalesDto>(false, "Máximo 5 vendedores por agencia.", null);

        var email = request.Email.ToLower().Trim();
        if (await userRepository.ExistsEmailAsync(email))
            return new ApiResponse<VendorSalesDto>(false, "El email ya está registrado.", null);

        var user = new User
        {
            Name = request.DisplayName.Trim(),
            Email = email,
            Role = "Vendedor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            CreatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(user);
        await partnerRepository.AddStaffAsync(new PartnerStaff
        {
            PartnerId = partnerId,
            UserId = user.Id,
            DisplayName = request.DisplayName.Trim(),
            StaffRole = "Vendedor"
        });
        return new ApiResponse<VendorSalesDto>(true, "Vendedor creado.", new VendorSalesDto(user.Id, user.Name, 0, 0));
    }

    public async Task<ApiResponse<TourDto>> CreateTourAsync(CreateTourRequest request, int userId, string role)
    {
        if (role is not ("Admin" or "Agencia" or "SuperAdmin"))
            return new ApiResponse<TourDto>(false, "Solo el administrador de agencia puede crear tours.", null);
        return await tourService.CreateAsync(request, userId);
    }

    public async Task<ApiResponse<bool>> UpdateTourCapacityAsync(int tourId, int partnerId, int available)
    {
        var tour = await tourRepository.GetByIdAsync(tourId);
        if (tour == null || tour.PartnerId != partnerId)
            return new ApiResponse<bool>(false, "Tour no encontrado.", false);
        tour.AvailableCapacity = available;
        await tourRepository.UpdateAsync(tour);
        return new ApiResponse<bool>(true, "Cupos actualizados.", true);
    }

    private static ReservationDto MapReservation(Reservation r) => new(
        r.Id, r.UserId, r.User?.Name ?? "", r.TourId, r.Tour?.Title ?? "", r.Tour?.Slug ?? "",
        r.Tour?.Location ?? "", r.Tour?.Date ?? DateTime.UtcNow, r.Quantity, r.Total, r.Commission,
        r.LoyaltyPointsEarned, r.Status, r.CreatedAt);
}
