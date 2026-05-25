using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.DTOs;
using System.Data;

namespace DestinoPeruAPI.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public interface IImageService
{
    Task<string> UploadImageAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);
    string OptimizeUrl(string? url, int width = 800);
}

public interface ITourQueryRepository
{
    Task<PagedResult<TourDto>> SearchPagedAsync(TourSearchQuery query);
    Task<TourDto?> GetByIdAsync(int id);
    Task<TourDto?> GetBySlugAsync(string slug);
}

public interface IPartnerQueryRepository
{
    Task<IReadOnlyList<PartnerDto>> GetPendingAsync();
    Task<AdminMetricsDto> GetAdminMetricsAsync();
    Task<SuperAdminMetricsDto> GetSuperAdminMetricsAsync();
    Task<IReadOnlyList<PartnerListItemDto>> GetAllPartnersListAsync();
    Task<AgencyDashboardDto?> GetAgencyDashboardAsync(int partnerId);
    Task<IReadOnlyList<AgencyRankingDto>> GetAgencyRankingAsync();
    Task<IReadOnlyList<CategoryMetricsDto>> GetCategoryMetricsAsync();
}
