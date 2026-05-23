using Dapper;
using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;

namespace DestinoPeruAPI.Infrastructure.Dapper;

public class TourQueryRepository(IDbConnectionFactory connectionFactory) : ITourQueryRepository
{
    private const string TourSelect = """
        SELECT t."Id", t."PartnerId", p."Name" AS "PartnerName", t."Slug", t."Title", t."Description",
               t."MetaTitle", t."MetaDescription", t."Price", t."Location", t."Department",
               t."AdventureType", t."Date", t."Capacity", t."AvailableCapacity",
               t."ImageUrl", t."IsActive", t."CreatedAt"
        FROM "Tours" t
        INNER JOIN "Partners" p ON p."Id" = t."PartnerId"
        """;

    public async Task<PagedResult<TourDto>> SearchPagedAsync(TourSearchQuery query)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        var where = """ WHERE t."IsActive" = true AND t."Date" > NOW() AND t."AvailableCapacity" > 0 """;
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            where += """ AND t."Department" ILIKE @Department """;
            parameters.Add("Department", $"%{query.Department}%");
        }
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            where += """ AND (t."Location" ILIKE @Location OR t."Title" ILIKE @Location) """;
            parameters.Add("Location", $"%{query.Location}%");
        }
        if (!string.IsNullOrWhiteSpace(query.AdventureType))
        {
            where += """ AND t."AdventureType" = @AdventureType """;
            parameters.Add("AdventureType", query.AdventureType);
        }
        if (query.FromDate.HasValue)
        {
            where += """ AND t."Date" >= @FromDate """;
            parameters.Add("FromDate", query.FromDate.Value);
        }
        if (query.MaxPrice.HasValue)
        {
            where += """ AND t."Price" <= @MaxPrice """;
            parameters.Add("MaxPrice", query.MaxPrice.Value);
        }

        var countSql = $"""
            SELECT COUNT(*)
            FROM "Tours" t
            INNER JOIN "Partners" p ON p."Id" = t."PartnerId"
            {where}
            """;
        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var offset = (page - 1) * pageSize;
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", offset);

        var sql = $"""
            {TourSelect}
            {where}
            ORDER BY t."Date" ASC
            LIMIT @Limit OFFSET @Offset
            """;

        var items = (await conn.QueryAsync<TourDto>(sql, parameters)).ToList();
        return new PagedResult<TourDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<TourDto?> GetByIdAsync(int id)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        var sql = $"""{TourSelect} WHERE t."Id" = @Id""";
        return await conn.QueryFirstOrDefaultAsync<TourDto>(sql, new { Id = id });
    }

    public async Task<TourDto?> GetBySlugAsync(string slug)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        var sql = $"""{TourSelect} WHERE t."Slug" = @Slug""";
        return await conn.QueryFirstOrDefaultAsync<TourDto>(sql, new { Slug = slug });
    }
}
