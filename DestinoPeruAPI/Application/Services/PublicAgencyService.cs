using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DestinoPeruAPI.Application.Services;

public class PublicAgencyService(AppDbContext db)
{
    public async Task<ApiResponse<AgencyPublicProfileDto>> GetBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return new ApiResponse<AgencyPublicProfileDto>(false, "Agencia no encontrada.", null);

        var partner = await db.Partners
            .FirstOrDefaultAsync(p => p.Slug != null && p.Slug.ToLower() == slug.ToLower());

        if (partner is null)
            return new ApiResponse<AgencyPublicProfileDto>(false, "Agencia no encontrada.", null);

        if (string.Equals(partner.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
            return new ApiResponse<AgencyPublicProfileDto>(false, "Esta agencia no está disponible.", null);

        var tours = await db.Tours
            .Where(t => t.PartnerId == partner.Id && t.IsActive)
            .OrderBy(t => t.Date)
            .Select(t => new AgencyTourListItemDto(
                t.Id, t.Title, t.Slug, t.Department, t.Price,
                t.AvailableCapacity, t.Capacity, t.IsActive, t.ImageUrl, t.AdventureType))
            .ToListAsync();

        var dto = new AgencyPublicProfileDto(
            partner.Id, partner.Slug!, partner.Name, partner.LogoUrl, partner.OperatingDepartment,
            partner.Status, string.Equals(partner.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase),
            partner.ContactPhone, partner.ContactEmail,
            tours.Count, partner.CreatedAt, tours);

        return new ApiResponse<AgencyPublicProfileDto>(true, null, dto);
    }
}
