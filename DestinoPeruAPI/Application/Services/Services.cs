using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IJwtService jwtService,
    ILoyaltyRepository loyaltyRepository,
    IPartnerRepository partnerRepository)
{
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await userRepository.ExistsEmailAsync(request.Email))
            return new ApiResponse<AuthResponse>(false, "El email ya está registrado.", null);

        var role = string.Equals(request.Role, "Cliente", StringComparison.OrdinalIgnoreCase)
            ? "Cliente" : "Cliente";

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            Role = role
        };
        await userRepository.AddAsync(user);
        await loyaltyRepository.AddPointsAsync(user.Id, 0);
        return await BuildAuthResponseAsync(user);
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.ToLower().Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return new ApiResponse<AuthResponse>(false, "Credenciales incorrectas.", null);
        return await BuildAuthResponseAsync(user);
    }

    private async Task<ApiResponse<AuthResponse>> BuildAuthResponseAsync(User user)
    {
        var partnerId = await ResolvePartnerIdAsync(user);
        var token = jwtService.GenerateToken(user.Id, user.Email, user.Role, user.Name, partnerId);
        return new ApiResponse<AuthResponse>(true, "Login exitoso.",
            new AuthResponse(token, user.Name, user.Email, user.Role, user.Id, partnerId));
    }

    private async Task<int?> ResolvePartnerIdAsync(User user)
    {
        if (user.Role is "Admin" or "Agencia")
        {
            var p = await partnerRepository.GetByUserIdAsync(user.Id);
            return p?.Id;
        }
        if (user.Role == "Vendedor")
        {
            var staff = await partnerRepository.GetStaffByUserIdAsync(user.Id);
            return staff?.PartnerId;
        }
        return null;
    }
}

public class TourService(
    ITourQueryRepository tourQuery,
    ITourRepository tourCommand,
    IPartnerRepository partnerRepository,
    IImageService imageService)
{
    public Task<PagedResult<TourDto>> SearchPagedAsync(TourSearchQuery query) => tourQuery.SearchPagedAsync(query);
    public Task<TourDto?> GetBySlugAsync(string slug) => tourQuery.GetBySlugAsync(slug);
    public async Task<ApiResponse<TourDto>> GetByIdAsync(int id)
    {
        var t = await tourQuery.GetByIdAsync(id);
        return t == null ? new ApiResponse<TourDto>(false, "Tour no encontrado.", null) : new ApiResponse<TourDto>(true, null, t);
    }

    public async Task<ApiResponse<TourDto>> CreateAsync(CreateTourRequest request, int userId)
    {
        var partner = await partnerRepository.GetByUserIdAsync(userId);
        if (partner == null) return new ApiResponse<TourDto>(false, "No tienes un partner registrado.", null);
        if (partner.Status != "Approved") return new ApiResponse<TourDto>(false, "Tu cuenta aún no está aprobada.", null);

        var slug = SlugHelper.Generate(request.Title);
        var existing = await tourCommand.GetBySlugAsync(slug);
        if (existing != null) slug += $"-{DateTime.UtcNow.Ticks % 10000}";

        var imageUrl = string.IsNullOrEmpty(request.ImageUrl) ? null : imageService.OptimizeUrl(request.ImageUrl);
        var tour = new Tour
        {
            PartnerId = partner.Id,
            Slug = slug,
            Title = request.Title,
            Description = request.Description,
            MetaTitle = request.MetaTitle ?? request.Title,
            MetaDescription = request.MetaDescription ?? request.Description[..Math.Min(160, request.Description.Length)],
            Price = request.Price,
            Location = request.Location,
            Department = request.Department,
            AdventureType = request.AdventureType,
            Date = request.Date,
            Capacity = request.Capacity,
            AvailableCapacity = request.Capacity,
            ImageUrl = imageUrl
        };
        await tourCommand.AddAsync(tour);
        var created = await tourQuery.GetByIdAsync(tour.Id);
        return new ApiResponse<TourDto>(true, "Tour creado.", created);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId, string role)
    {
        var tour = await tourCommand.GetByIdAsync(id);
        if (tour == null) return new ApiResponse<bool>(false, "Tour no encontrado.", false);
        if (role != "SuperAdmin")
        {
            var partner = await partnerRepository.GetByUserIdAsync(userId);
            if (partner == null || tour.PartnerId != partner.Id)
                return new ApiResponse<bool>(false, "Sin permiso.", false);
        }
        await tourCommand.DeleteAsync(id);
        return new ApiResponse<bool>(true, "Tour eliminado.", true);
    }
}

public class PartnerService(IPartnerRepository partnerRepository, IPartnerQueryRepository partnerQuery, IUserRepository userRepository)
{
    public Task<IReadOnlyList<PartnerDto>> GetPendingAsync() => partnerQuery.GetPendingAsync();
    public Task<AdminMetricsDto> GetAdminMetricsAsync() => partnerQuery.GetAdminMetricsAsync();

    public async Task<ApiResponse<PartnerDto>> CreateAsync(CreatePartnerRequest request, int userId)
    {
        var existing = await partnerRepository.GetByUserIdAsync(userId);
        if (existing != null) return new ApiResponse<PartnerDto>(false, "Ya tienes un partner registrado.", null);
        var partner = new Partner
        {
            UserId = userId,
            Name = request.Name,
            RUC = request.RUC,
            PartnerType = request.PartnerType
        };
        await partnerRepository.AddAsync(partner);
        var user = await userRepository.GetByIdAsync(userId);
        user!.Role = "Agencia";
        await userRepository.UpdateAsync(user);
        return new ApiResponse<PartnerDto>(true, "Partner registrado. Pendiente de verificación.", MapToDto(partner, user.Name));
    }

    public async Task<ApiResponse<PartnerDto>> ApproveAsync(int id)
    {
        var partner = await partnerRepository.GetByIdAsync(id);
        if (partner == null) return new ApiResponse<PartnerDto>(false, "Partner no encontrado.", null);
        partner.Status = "Approved";
        partner.VerificationStatus = "Verified";
        await partnerRepository.UpdateAsync(partner);
        return new ApiResponse<PartnerDto>(true, "Partner aprobado.", MapToDto(partner, partner.User?.Name ?? ""));
    }

    public async Task<ApiResponse<bool>> AddDocumentAsync(int partnerId, int userId, DocumentType type, string fileUrl)
    {
        var partner = await partnerRepository.GetByUserIdAsync(userId);
        if (partner == null || partner.Id != partnerId)
            return new ApiResponse<bool>(false, "Sin permiso.", false);
        await partnerRepository.AddDocumentAsync(new PartnerDocument
        {
            PartnerId = partnerId,
            DocumentType = type,
            FileUrl = fileUrl,
            Status = "Pending"
        });
        return new ApiResponse<bool>(true, "Documento registrado.", true);
    }

    public async Task<ApiResponse<bool>> VerifyDocumentAsync(int documentId, string adminName, bool approved)
    {
        var doc = await partnerRepository.GetDocumentAsync(documentId);
        if (doc == null) return new ApiResponse<bool>(false, "Documento no encontrado.", false);
        doc.Status = approved ? "Approved" : "Rejected";
        doc.ReviewedBy = adminName;
        doc.ReviewedAt = DateTime.UtcNow;
        await partnerRepository.UpdateDocumentAsync(doc);
        return new ApiResponse<bool>(true, "Documento revisado.", true);
    }

    private static PartnerDto MapToDto(Partner p, string userName) => new(
        p.Id, p.UserId, userName, p.Name, p.RUC, p.PartnerType, p.Status, p.VerificationStatus, p.CommissionRate, p.CreatedAt);
}
