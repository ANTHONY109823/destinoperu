using DestinoPeruAPI.Application.Common;
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
    TourService tourService,
    AppDbContext appDb)
{
    public async Task<SuperAdminMetricsDto> GetMetricsAsync()
    {
        try { return await partnerQuery.GetSuperAdminMetricsAsync(); }
        catch
        {
            var all = (await partnerRepository.GetAllAsync()).ToList();
            return new SuperAdminMetricsDto(
                await appDb.Users.CountAsync(), all.Count,
                all.Count(p => p.Status == "Pending"),
                await appDb.Tours.CountAsync(t => t.IsActive),
                await appDb.Reservations.CountAsync(), 0, 0,
                await appDb.Users.CountAsync(u => u.Role == RoleNames.Cliente));
        }
    }

    public Task<IReadOnlyList<AgencyRankingDto>> GetRankingAsync() => partnerQuery.GetAgencyRankingAsync();

    public async Task<SuperAdminDashboardDto> GetDashboardAsync()
    {
        var global = await GetMetricsAsync();
        try
        {
            var categories = await partnerQuery.GetCategoryMetricsAsync();
            return new SuperAdminDashboardDto(global, categories);
        }
        catch
        {
            var cats = CategoryCatalog.All.Select(c => new CategoryMetricsDto(
                c.Key, c.Label, c.Icon, 0, 0, 0, 0, 0)).ToList();
            return new SuperAdminDashboardDto(global, cats);
        }
    }

    public IReadOnlyList<PartnerListItemDto> FilterPartnersByCategory(
        IReadOnlyList<PartnerListItemDto> partners, string? categoryKey)
    {
        var cat = CategoryCatalog.FromKey(categoryKey);
        if (cat is null) return partners;
        return partners.Where(p => p.PartnerType == cat.PartnerType).ToList();
    }

    public async Task<IReadOnlyList<PartnerListItemDto>> GetPartnersAsync()
    {
        try { return await partnerQuery.GetAllPartnersListAsync(); }
        catch { return await GetPartnersFromEfAsync(); }
    }

    public async Task<IReadOnlyList<AgencyStaffDto>> GetPartnerStaffAsync(int partnerId)
    {
        var staff = await partnerRepository.GetStaffByPartnerIdAsync(partnerId);
        return staff.Select(s => new AgencyStaffDto(
            s.UserId, s.User.Name, s.User.Email, s.DisplayName, s.StaffRole)).ToList();
    }

    private async Task<IReadOnlyList<PartnerListItemDto>> GetPartnersFromEfAsync()
    {
        var list = new List<PartnerListItemDto>();
        foreach (var p in await partnerRepository.GetAllAsync())
        {
            var u = p.User;
            var tourCount = await appDb.Tours.CountAsync(t => t.PartnerId == p.Id && t.IsActive);
            list.Add(new PartnerListItemDto(
                p.Id, p.Name, p.RUC, p.Status, p.OperatingDepartment ?? "",
                u?.Email ?? "", u?.Name ?? "", u?.Id ?? 0,
                await partnerRepository.GetStaffCountAsync(p.Id), 0,
                p.PartnerType, tourCount));
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
            Role = RoleNames.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword, 12),
            CreatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(admin);

        var partner = new Partner
        {
            UserId = admin.Id,
            Name = request.AgencyName.Trim(),
            RUC = request.RUC.Trim(),
            PartnerType = request.PartnerType,
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
        return new ApiResponse<PartnerDto>(true, "Agencia y administrador inicial creados.", MapPartner(partner, admin.Name));
    }

    public async Task<ApiResponse<PartnerDto>> UpdateAgencyAsync(int partnerId, UpdateAgencyRequest request)
    {
        var partner = await partnerRepository.GetByIdAsync(partnerId);
        if (partner == null) return new ApiResponse<PartnerDto>(false, "Agencia no encontrada.", null);

        if (!string.IsNullOrWhiteSpace(request.Name)) partner.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.RUC)) partner.RUC = request.RUC.Trim();
        if (request.OperatingDepartment != null) partner.OperatingDepartment = request.OperatingDepartment;
        if (request.LogoUrl != null) partner.LogoUrl = request.LogoUrl;
        if (request.ContactEmail != null) partner.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) partner.ContactPhone = request.ContactPhone;
        if (!string.IsNullOrWhiteSpace(request.Status)) partner.Status = request.Status;

        await partnerRepository.UpdateAsync(partner);
        return new ApiResponse<PartnerDto>(true, "Agencia actualizada.", MapPartner(partner, partner.User?.Name ?? ""));
    }

    public async Task<ApiResponse<bool>> SuspendPartnerAsync(int partnerId, bool suspend)
    {
        var r = await UpdateAgencyAsync(partnerId, new UpdateAgencyRequest(null, null, null, null, null, null, suspend ? "Suspended" : "Approved"));
        return r.Success
            ? new ApiResponse<bool>(true, suspend ? "Agencia suspendida." : "Agencia reactivada.", true)
            : new ApiResponse<bool>(false, r.Message, false);
    }

    public async Task<ApiResponse<AuthResponse>> ImpersonateAsync(int targetUserId)
    {
        if (targetUserId <= 0)
            return new ApiResponse<AuthResponse>(false, "ID de usuario inválido.", null);

        var target = await userRepository.GetByIdAsync(targetUserId);
        if (target == null) return new ApiResponse<AuthResponse>(false, "Usuario no encontrado.", null);

        int? partnerId = null;
        if (target.Role is RoleNames.Admin or RoleNames.Agencia)
        {
            var p = await partnerRepository.GetByUserIdAsync(target.Id);
            partnerId = p?.Id;
            if (!partnerId.HasValue)
                return new ApiResponse<AuthResponse>(false, "Este usuario no tiene agencia vinculada.", null);
        }
        else if (target.Role == RoleNames.Vendedor)
        {
            var staff = await partnerRepository.GetStaffByUserIdAsync(target.Id);
            partnerId = staff?.PartnerId;
            if (!partnerId.HasValue)
                return new ApiResponse<AuthResponse>(false, "Vendedor sin agencia asignada.", null);
        }
        else
            return new ApiResponse<AuthResponse>(false, "Solo puedes suplantar Admin, Agencia o Vendedor.", null);

        var token = jwtService.GenerateToken(target.Id, target.Email, target.Role, target.Name, partnerId, impersonating: true);
        return new ApiResponse<AuthResponse>(true, "Modo soporte activado.",
            new AuthResponse(token, target.Name, target.Email, target.Role, target.Id, partnerId, true));
    }

    public async Task<ApiResponse<TourDto>> CreateTourForPartnerAsync(int partnerId, CreateTourRequest request) =>
        await tourService.CreateForPartnerAsync(request, partnerId);

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
    TourService tourService,
    AppDbContext appDb)
{
    public async Task<int?> ResolvePartnerIdAsync(int userId, string role)
    {
        if (role == RoleNames.SuperAdmin) return null;
        if (role is RoleNames.Admin or RoleNames.Agencia)
        {
            var p = await partnerRepository.GetByUserIdAsync(userId);
            return p?.Id;
        }
        if (role == RoleNames.Vendedor)
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
        catch { /* EF fallback */ }

        var partner = await partnerRepository.GetWithToursAsync(partnerId);
        if (partner is null) return new ApiResponse<AgencyDashboardDto>(false, "Agencia no encontrada.", null);
        return new ApiResponse<AgencyDashboardDto>(true, null, new AgencyDashboardDto(
            partner.Id, partner.Name, partner.Tours.Count(t => t.IsActive), 0, 0, 0, 0, []));
    }

    public async Task<ApiResponse<AgencyProfileDto>> GetProfileAsync(int partnerId)
    {
        var p = await partnerRepository.GetByIdAsync(partnerId);
        if (p is null) return new ApiResponse<AgencyProfileDto>(false, "Agencia no encontrada.", null);
        return new ApiResponse<AgencyProfileDto>(true, null, new AgencyProfileDto(
            p.Id, p.Name, p.RUC, p.LogoUrl, p.OperatingDepartment,
            p.ContactEmail, p.ContactPhone, p.Status, p.CommissionRate));
    }

    public async Task<ApiResponse<AgencyProfileDto>> UpdateProfileAsync(int partnerId, UpdateAgencyRequest request, string role)
    {
        if (role is not (RoleNames.Admin or RoleNames.SuperAdmin))
            return new ApiResponse<AgencyProfileDto>(false, "Solo el administrador puede editar el perfil.", null);

        var p = await partnerRepository.GetByIdAsync(partnerId);
        if (p is null) return new ApiResponse<AgencyProfileDto>(false, "Agencia no encontrada.", null);

        if (!string.IsNullOrWhiteSpace(request.Name)) p.Name = request.Name!.Trim();
        if (request.LogoUrl != null) p.LogoUrl = request.LogoUrl;
        if (request.OperatingDepartment != null) p.OperatingDepartment = request.OperatingDepartment;
        if (request.ContactEmail != null) p.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) p.ContactPhone = request.ContactPhone;

        await partnerRepository.UpdateAsync(p);
        return await GetProfileAsync(partnerId);
    }

    public async Task<ApiResponse<List<AgencyTourListItemDto>>> GetToursAsync(int partnerId)
    {
        var tours = await appDb.Tours.Where(t => t.PartnerId == partnerId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AgencyTourListItemDto(
                t.Id, t.Title, t.Slug, t.Department, t.Price,
                t.AvailableCapacity, t.Capacity, t.IsActive, t.ImageUrl, t.AdventureType))
            .ToListAsync();
        return new ApiResponse<List<AgencyTourListItemDto>>(true, null, tours);
    }

    public async Task<ApiResponse<List<ReservationDto>>> GetReservationsAsync(int partnerId) =>
        new(true, null, (await reservationRepository.GetByPartnerAsync(partnerId)).Select(MapReservation).ToList());

    public async Task<ApiResponse<bool>> SetReservationStatusAsync(int reservationId, int partnerId, string status)
    {
        var list = await reservationRepository.GetByPartnerAsync(partnerId);
        var r = list.FirstOrDefault(x => x.Id == reservationId);
        if (r == null) return new ApiResponse<bool>(false, "Reserva no encontrada.", false);
        r.Status = status;
        await reservationRepository.UpdateAsync(r);
        return new ApiResponse<bool>(true, $"Reserva {status}.", true);
    }

    public async Task<ApiResponse<VendorSalesDto>> CreateVendorAsync(int partnerId, CreateVendorRequest request, string role)
    {
        if (role is not (RoleNames.Admin or RoleNames.SuperAdmin))
            return new ApiResponse<VendorSalesDto>(false, "Solo el administrador puede crear vendedores.", null);

        if (await partnerRepository.GetStaffCountAsync(partnerId) >= 5)
            return new ApiResponse<VendorSalesDto>(false, "Máximo 5 vendedores por agencia.", null);

        var email = request.Email.ToLower().Trim();
        if (await userRepository.ExistsEmailAsync(email))
            return new ApiResponse<VendorSalesDto>(false, "El email ya está registrado.", null);

        var user = new User
        {
            Name = request.DisplayName.Trim(),
            Email = email,
            Role = RoleNames.Vendedor,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            CreatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(user);
        await partnerRepository.AddStaffAsync(new PartnerStaff
        {
            PartnerId = partnerId,
            UserId = user.Id,
            DisplayName = request.DisplayName.Trim(),
            StaffRole = RoleNames.Vendedor
        });
        return new ApiResponse<VendorSalesDto>(true, "Vendedor creado.", new VendorSalesDto(user.Id, user.Name, 0, 0));
    }

    public async Task<ApiResponse<TourDto>> CreateTourAsync(CreateTourRequest request, int userId, string role, int? partnerIdFromHeader)
    {
        if (role is not (RoleNames.Admin or RoleNames.SuperAdmin))
            return new ApiResponse<TourDto>(false, "Solo el administrador de agencia puede crear tours.", null);

        if (role == RoleNames.SuperAdmin && partnerIdFromHeader.HasValue)
            return await tourService.CreateForPartnerAsync(request, partnerIdFromHeader.Value);

        return await tourService.CreateAsync(request, userId);
    }

    public async Task<ApiResponse<bool>> UpdateTourCapacityAsync(int tourId, int partnerId, int available, string role)
    {
        if (role is not (RoleNames.Admin or RoleNames.Vendedor or RoleNames.SuperAdmin))
            return new ApiResponse<bool>(false, "Sin permiso.", false);

        var tour = await tourRepository.GetByIdAsync(tourId);
        if (tour == null || tour.PartnerId != partnerId)
            return new ApiResponse<bool>(false, "Tour no encontrado.", false);
        tour.AvailableCapacity = Math.Clamp(available, 0, tour.Capacity);
        await tourRepository.UpdateAsync(tour);
        return new ApiResponse<bool>(true, "Cupos actualizados.", true);
    }

    public async Task<ApiResponse<bool>> UpdateTourItemAsync(int tourId, int partnerId, UpdateTourItemRequest request, string role)
    {
        if (role is not (RoleNames.Admin or RoleNames.Vendedor or RoleNames.SuperAdmin))
            return new ApiResponse<bool>(false, "Sin permiso.", false);

        var tour = await tourRepository.GetByIdAsync(tourId);
        if (tour == null || tour.PartnerId != partnerId)
            return new ApiResponse<bool>(false, "Tour no encontrado.", false);

        if (request.ImageUrl is not null) tour.ImageUrl = request.ImageUrl;
        if (request.AvailableCapacity.HasValue)
            tour.AvailableCapacity = Math.Clamp(request.AvailableCapacity.Value, 0, tour.Capacity);

        await tourRepository.UpdateAsync(tour);
        return new ApiResponse<bool>(true, "Tour actualizado.", true);
    }

    public async Task<ApiResponse<ManifestDto>> GetManifestAsync(int partnerId, int? tourId)
    {
        var reservations = (await reservationRepository.GetByPartnerAsync(partnerId))
            .Where(r => r.Status is "Confirmed" or "Paid")
            .Where(r => !tourId.HasValue || r.TourId == tourId.Value)
            .ToList();

        if (reservations.Count == 0)
            return new ApiResponse<ManifestDto>(false, "No hay pasajeros confirmados.", null);

        var first = reservations[0];
        var lines = new List<ManifestPassengerLineDto>();
        foreach (var r in reservations)
        {
            if (r.Passengers?.Count > 0)
            {
                foreach (var p in r.Passengers)
                    lines.Add(new ManifestPassengerLineDto(p.FullName, p.Dni, p.PickupPoint, 1));
            }
            else
                lines.Add(new ManifestPassengerLineDto(r.User?.Name ?? "Pasajero", "—", "—", r.Quantity));
        }

        var partner = await partnerRepository.GetByIdAsync(partnerId);
        return new ApiResponse<ManifestDto>(true, null, new ManifestDto(
            first.Tour?.Title ?? "Tour",
            first.Tour?.Date ?? DateTime.UtcNow,
            partner?.Name ?? "Agencia",
            lines));
    }

    private static ReservationDto MapReservation(Reservation r) => new(
        r.Id, r.UserId, r.User?.Name ?? "", r.TourId, r.Tour?.Title ?? "", r.Tour?.Slug ?? "",
        r.Tour?.Location ?? "", r.Tour?.Date ?? DateTime.UtcNow, r.Quantity, r.Total, r.Commission,
        r.LoyaltyPointsEarned, r.Status, r.CreatedAt);
}

public class UserAccountService(ILoyaltyRepository loyaltyRepository)
{
    public async Task<ApiResponse<LoyaltyDto>> GetLoyaltyAsync(int userId)
    {
        var account = await loyaltyRepository.GetByUserAsync(userId);
        return new ApiResponse<LoyaltyDto>(true, null, new LoyaltyDto(account?.Points ?? 0, account?.LifetimePoints ?? 0));
    }
}
