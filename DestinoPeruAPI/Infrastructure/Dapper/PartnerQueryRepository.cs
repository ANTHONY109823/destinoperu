using Dapper;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Infrastructure.Dapper;

public class PartnerQueryRepository(IDbConnectionFactory connectionFactory) : IPartnerQueryRepository
{
    public async Task<IReadOnlyList<PartnerDto>> GetPendingAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = """
            SELECT p."Id", p."UserId", u."Name" AS "UserName", p."Name", p."RUC",
                   p."PartnerType", p."Status", p."VerificationStatus", p."CommissionRate", p."CreatedAt",
                   (SELECT COUNT(*) FROM "PartnerDocuments" d WHERE d."PartnerId" = p."Id") AS "DocumentCount"
            FROM "Partners" p
            INNER JOIN "Users" u ON u."Id" = p."UserId"
            WHERE p."Status" = 'Pending' OR p."VerificationStatus" = 'Pending'
            ORDER BY p."CreatedAt" DESC
            """;
        return (await conn.QueryAsync<PartnerDto>(sql)).ToList();
    }

    public async Task<AdminMetricsDto> GetAdminMetricsAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM "Partners") AS "TotalPartners",
                (SELECT COUNT(*) FROM "Partners" WHERE "Status" = 'Pending') AS "PendingPartners",
                (SELECT COUNT(*) FROM "Tours" WHERE "IsActive" = true) AS "TotalTours",
                (SELECT COUNT(*) FROM "Reservations") AS "TotalReservations",
                COALESCE((SELECT SUM("Commission") FROM "Reservations" WHERE "Status" = 'Paid'), 0) AS "TotalCommissions",
                (SELECT COUNT(*) FROM "Users") AS "ActiveUsers"
            """;
        return await conn.QueryFirstAsync<AdminMetricsDto>(sql);
    }
}
