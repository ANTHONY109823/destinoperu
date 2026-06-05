using Dapper;
using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Interfaces;

namespace DestinoPeruAPI.Infrastructure.Dapper;

/// <summary>Lecturas Dapper — tablas EF: "Tours" + "Partners" (no Agencies).</summary>
public class TourQueryRepository(IDbConnectionFactory connectionFactory) : ITourQueryRepository
{
    private const string TourSelect = """
        SELECT t."Id", t."PartnerId", p."Name" AS "PartnerName", p."Slug" AS "PartnerSlug", t."Slug", t."Title", t."Description",
               t."MetaTitle", t."MetaDescription", t."Price", t."Location", t."Department",
               t."AdventureType", t."Date", t."Capacity", t."AvailableCapacity",
               t."ImageUrl", t."IsActive", t."CreatedAt",
               t."PuntoPartida", t."PuntoRetorno", t."HoraSalida", t."DuracionAproximada",
               t."ItinerarioJson", t."QueIncluyeJson", t."QueNoIncluyeJson", t."QueLlevarJson", t."GaleriaJson"
        FROM "Tours" t
        INNER JOIN "Partners" p ON p."Id" = t."PartnerId"
        """;

    private sealed class TourRow
    {
        public int Id { get; set; }
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = "";
        public string? PartnerSlug { get; set; }
        public string Slug { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string MetaTitle { get; set; } = "";
        public string MetaDescription { get; set; } = "";
        public decimal Price { get; set; }
        public string Location { get; set; } = "";
        public string Department { get; set; } = "";
        public string AdventureType { get; set; } = "";
        public DateTime Date { get; set; }
        public int Capacity { get; set; }
        public int AvailableCapacity { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PuntoPartida { get; set; }
        public string? PuntoRetorno { get; set; }
        public string? HoraSalida { get; set; }
        public string? DuracionAproximada { get; set; }
        public string? ItinerarioJson { get; set; }
        public string? QueIncluyeJson { get; set; }
        public string? QueNoIncluyeJson { get; set; }
        public string? QueLlevarJson { get; set; }
        public string? GaleriaJson { get; set; }
    }

    private static TourDto MapRow(TourRow r) => new(
        r.Id, r.PartnerId, r.PartnerName, r.Slug, r.Title, r.Description,
        r.MetaTitle, r.MetaDescription, r.Price, r.Location, r.Department,
        r.AdventureType, r.Date, r.Capacity, r.AvailableCapacity,
        r.ImageUrl, r.IsActive, r.CreatedAt,
        r.PuntoPartida, r.PuntoRetorno, r.HoraSalida, r.DuracionAproximada,
        TourContentMapper.DeserializeItinerary(r.ItinerarioJson),
        TourContentMapper.DeserializeStrings(r.QueIncluyeJson),
        TourContentMapper.DeserializeStrings(r.QueNoIncluyeJson),
        TourContentMapper.DeserializeStrings(r.QueLlevarJson),
        TourContentMapper.DeserializeStrings(r.GaleriaJson),
        r.PartnerSlug);

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
        if (query.ToDate.HasValue)
        {
            where += """ AND t."Date" <= @ToDate """;
            parameters.Add("ToDate", query.ToDate.Value);
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

        var rows = (await conn.QueryAsync<TourRow>(sql, parameters)).ToList();
        var items = rows.Select(MapRow).ToList();
        return new PagedResult<TourDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<TourDto?> GetByIdAsync(int id)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        var sql = $"""{TourSelect} WHERE t."Id" = @Id""";
        var row = await conn.QueryFirstOrDefaultAsync<TourRow>(sql, new { Id = id });
        return row is null ? null : MapRow(row);
    }

    public async Task<TourDto?> GetBySlugAsync(string slug)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        var sql = $"""{TourSelect} WHERE t."Slug" = @Slug""";
        var row = await conn.QueryFirstOrDefaultAsync<TourRow>(sql, new { Slug = slug });
        return row is null ? null : MapRow(row);
    }
}
