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
        conn.Open();
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
        conn.Open();
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

    public async Task<SuperAdminMetricsDto> GetSuperAdminMetricsAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM "Users") AS "TotalUsers",
                (SELECT COUNT(*) FROM "Partners") AS "TotalPartners",
                (SELECT COUNT(*) FROM "Partners" WHERE "Status" = 'Pending') AS "PendingPartners",
                (SELECT COUNT(*) FROM "Tours" WHERE "IsActive" = true) AS "TotalTours",
                (SELECT COUNT(*) FROM "Reservations") AS "TotalReservations",
                COALESCE((SELECT SUM("Total") FROM "Reservations" WHERE "Status" IN ('Paid', 'Confirmed')), 0) AS "TotalRevenue",
                COALESCE((SELECT SUM("Commission") FROM "Reservations" WHERE "Status" IN ('Paid', 'Confirmed')), 0) AS "TotalCommissions",
                (SELECT COUNT(*) FROM "Users" WHERE "Role" = 'Cliente') AS "ActiveUsers"
            """;
        return await conn.QueryFirstAsync<SuperAdminMetricsDto>(sql);
    }

    public async Task<IReadOnlyList<PartnerListItemDto>> GetAllPartnersListAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        const string sql = """
            SELECT p."Id", p."Name", p."RUC", p."Status",
                   COALESCE(p."OperatingDepartment", '') AS "OperatingDepartment",
                   u."Email" AS "AdminEmail", u."Name" AS "AdminName", u."Id" AS "AdminUserId",
                   (SELECT COUNT(*) FROM "PartnerStaff" s WHERE s."PartnerId" = p."Id") AS "StaffCount",
                   COALESCE((
                       SELECT SUM(r."Total") FROM "Reservations" r
                       INNER JOIN "Tours" t ON t."Id" = r."TourId"
                       WHERE t."PartnerId" = p."Id" AND r."Status" IN ('Paid', 'Confirmed')
                   ), 0) AS "Revenue"
            FROM "Partners" p
            INNER JOIN "Users" u ON u."Id" = p."UserId"
            ORDER BY p."CreatedAt" DESC
            """;
        return (await conn.QueryAsync<PartnerListItemDto>(sql)).ToList();
    }

    public async Task<AgencyDashboardDto?> GetAgencyDashboardAsync(int partnerId)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        const string sql = """
            SELECT p."Id" AS "PartnerId", p."Name" AS "PartnerName",
                (SELECT COUNT(*) FROM "Tours" t WHERE t."PartnerId" = p."Id" AND t."IsActive" = true) AS "TotalTours",
                (SELECT COUNT(*) FROM "Reservations" r INNER JOIN "Tours" t ON t."Id" = r."TourId"
                    WHERE t."PartnerId" = p."Id" AND r."Status" = 'Pending') AS "PendingReservations",
                (SELECT COUNT(*) FROM "Reservations" r INNER JOIN "Tours" t ON t."Id" = r."TourId"
                    WHERE t."PartnerId" = p."Id" AND r."Status" IN ('Confirmed', 'Paid')) AS "ConfirmedReservations",
                COALESCE((SELECT SUM(r."Total") FROM "Reservations" r INNER JOIN "Tours" t ON t."Id" = r."TourId"
                    WHERE t."PartnerId" = p."Id" AND r."Status" IN ('Paid', 'Confirmed')), 0) AS "TotalRevenue",
                COALESCE((SELECT SUM(r."Commission") FROM "Reservations" r INNER JOIN "Tours" t ON t."Id" = r."TourId"
                    WHERE t."PartnerId" = p."Id" AND r."Status" IN ('Paid', 'Confirmed')), 0) AS "AgencyCommissions"
            FROM "Partners" p WHERE p."Id" = @partnerId
            """;
        var dash = await conn.QueryFirstOrDefaultAsync<AgencyDashboardDto>(sql, new { partnerId });
        if (dash is null) return null;

        var vendors = new List<VendorSalesDto>();
        try
        {
            const string vendorSql = """
                SELECT u."Id" AS "UserId", COALESCE(s."DisplayName", u."Name") AS "Name",
                    COUNT(r."Id")::int AS "Reservations",
                    COALESCE(SUM(r."Total"), 0) AS "Revenue"
                FROM "PartnerStaff" s
                INNER JOIN "Users" u ON u."Id" = s."UserId"
                LEFT JOIN "Reservations" r ON r."UserId" = u."Id"
                LEFT JOIN "Tours" t ON t."Id" = r."TourId" AND t."PartnerId" = s."PartnerId"
                WHERE s."PartnerId" = @partnerId
                GROUP BY u."Id", s."DisplayName", u."Name"
                """;
            vendors = (await conn.QueryAsync<VendorSalesDto>(vendorSql, new { partnerId })).ToList();
        }
        catch
        {
            /* PartnerStaff puede no existir hasta aplicar migración */
        }

        return dash with { VendorSales = vendors };
    }

    public async Task<IReadOnlyList<AgencyRankingDto>> GetAgencyRankingAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        const string sql = """
            SELECT p."Id" AS "PartnerId", p."Name",
                COALESCE((
                    SELECT SUM(r."Total") FROM "Reservations" r
                    INNER JOIN "Tours" t ON t."Id" = r."TourId"
                    WHERE t."PartnerId" = p."Id" AND r."Status" IN ('Paid', 'Confirmed')
                ), 0) AS "Revenue",
                (SELECT COUNT(*) FROM "Reservations" r
                    INNER JOIN "Tours" t ON t."Id" = r."TourId"
                    WHERE t."PartnerId" = p."Id") AS "Reservations",
                (SELECT COUNT(*) FROM "Tours" t WHERE t."PartnerId" = p."Id" AND t."IsActive" = true) AS "TourCount"
            FROM "Partners" p
            ORDER BY "Revenue" DESC
            """;
        return (await conn.QueryAsync<AgencyRankingDto>(sql)).ToList();
    }
}
