using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DestinoPeruAPI.Infrastructure.Data;

public static class DbInitializer
{
    private const string DemoEmail = "demo@destinoperu.com";
    private const int MinPresentationTours = 18;

    private const string SuperAdminEmail = "superadmin@destinoperu.com";
    private const string AgencyAdminEmail = "admin@destinoperu.com";
    private const string SystemPassword = "Admin2026!";

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await EnsureSystemAdminsAsync(db, logger);
        await EnsureDemoUserAsync(db, logger);

        var tourCount = await db.Tours.CountAsync();
        if (tourCount >= MinPresentationTours)
        {
            logger.LogInformation("Catálogo con {Count} tours; seed de presentación omitido.", tourCount);
            return;
        }

        logger.LogInformation("Sembrando contenido DEMO de presentación (partners + tours)...");
        var partners = await EnsureDemoPartnersAsync(db);
        if (partners.Count == 0)
        {
            logger.LogWarning("No se pudieron crear partners demo.");
            return;
        }

        var existingSlugs = await db.Tours.Select(t => t.Slug).ToListAsync();
        var tours = BuildPresentationTours(partners, existingSlugs);
        if (tours.Count > 0)
        {
            db.Tours.AddRange(tours);
            await db.SaveChangesAsync();
            logger.LogInformation("Seed presentación: +{Tours} tours. Total partners: {Partners}.",
                tours.Count, partners.Count);
        }
    }

    private static async Task EnsureSystemAdminsAsync(AppDbContext db, ILogger logger)
    {
        await UpsertUserAsync(db, SuperAdminEmail, "Super Admin DestinoPerú", "SuperAdmin", SystemPassword, logger);

        var agencyAdmin = await UpsertUserAsync(db, AgencyAdminEmail, "Admin Agencia Demo", "Admin", SystemPassword, logger);
        var agencyPartner = await db.Partners.FirstOrDefaultAsync(p => p.UserId == agencyAdmin.Id);
        if (agencyPartner is null)
        {
            agencyPartner = new Partner
            {
                UserId = agencyAdmin.Id,
                Name = "Agencia Demo DestinoPerú",
                RUC = "20999999991",
                PartnerType = PartnerType.Agencia,
                Status = "Approved",
                VerificationStatus = "Verified",
                OperatingDepartment = "Lima",
                ContactEmail = AgencyAdminEmail,
                CommissionRate = 0.10m,
                CreatedAt = DateTime.UtcNow
            };
            db.Partners.Add(agencyPartner);
            await db.SaveChangesAsync();
            logger.LogInformation("Partner demo vinculado a {Email}", AgencyAdminEmail);
        }
        else
        {
            agencyPartner.Status = "Approved";
            agencyPartner.OperatingDepartment ??= "Lima";
            await db.SaveChangesAsync();
        }
    }

    private static async Task<User> UpsertUserAsync(AppDbContext db, string email, string name, string role, string password, ILogger logger)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        if (user is null)
        {
            user = new User { Name = name, Email = email, Role = role, PasswordHash = hash, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            logger.LogInformation("Usuario sistema creado: {Email} ({Role})", email, role);
            return user;
        }
        user.Name = name;
        user.Role = role;
        user.PasswordHash = hash;
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task EnsureDemoUserAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Users.AnyAsync(u => u.Email == DemoEmail))
            return;

        db.Users.Add(new User
        {
            Name = "Usuario Demo",
            Email = DemoEmail,
            Role = "Cliente",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!", 12),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Usuario demo creado: {Email}", DemoEmail);
    }

    private static async Task<List<Partner>> EnsureDemoPartnersAsync(AppDbContext db)
    {
        var specs = new (string Name, string Email, string Ruc, string City, string Dept)[]
        {
            ("Cusco Andes Travel", "cusco.andes@demo.dp", "20100010001", "Cusco", "Cusco"),
            ("Machu Picchu Expeditions", "machu.exp@demo.dp", "20100010002", "Cusco", "Cusco"),
            ("Lima City Tours", "lima.city@demo.dp", "20100010003", "Lima", "Lima"),
            ("Costa Verde Aventuras", "costa.verde@demo.dp", "20100010004", "Lima", "Lima"),
            ("Arequipa Volcano Tours", "arequipa.vol@demo.dp", "20100010005", "Arequipa", "Arequipa"),
            ("Colca Canyon Adventures", "colca@demo.dp", "20100010006", "Arequipa", "Arequipa"),
            ("Ica Dunas y Vinos", "ica.dunas@demo.dp", "20100010007", "Ica", "Ica"),
            ("Huacachina Sunset", "huacachina@demo.dp", "20100010008", "Ica", "Ica"),
            ("Puno Titicaca Experience", "puno.titi@demo.dp", "20100010009", "Puno", "Puno"),
            ("Uros Floating Tours", "uros@demo.dp", "20100010010", "Puno", "Puno"),
            ("Loreto Amazonia Verde", "loreto.amz@demo.dp", "20100010011", "Iquitos", "Loreto"),
            ("Selva Premium Lodge", "selva@demo.dp", "20100010012", "Tarapoto", "San Martin")
        };

        var partners = new List<Partner>();
        foreach (var s in specs)
        {
            if (await db.Partners.AnyAsync(p => p.RUC == s.Ruc))
            {
                var existing = await db.Partners.FirstAsync(p => p.RUC == s.Ruc);
                partners.Add(existing);
                continue;
            }

            var user = new User
            {
                Name = s.Name,
                Email = s.Email,
                Role = "Agencia",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!", 12),
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var partner = new Partner
            {
                UserId = user.Id,
                Name = s.Name,
                RUC = s.Ruc,
                PartnerType = PartnerType.Agencia,
                Status = "Approved",
                VerificationStatus = "Verified",
                CommissionRate = 0.10m,
                CreatedAt = DateTime.UtcNow
            };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();
            partners.Add(partner);
        }

        return partners;
    }

    private static List<Tour> BuildPresentationTours(List<Partner> partners, List<string> existingSlugs)
    {
        var byDept = partners.GroupBy(p => p.Name).ToList();
        Partner Pick(int i) => partners[i % partners.Count];

        var defs = new (string Slug, string Title, string Desc, string Loc, string Dept, string Type, decimal Price, string Img, int Days)[]
        {
            ("full-day-paracas-huacachina", "Full Day Paracas e Huacachina", "Islas Ballestas, reserva y oasis con sandboard.", "Paracas", "Ica", "FullDay", 189m, "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 5),
            ("city-tour-lima-centro", "City Tour Lima Centro Histórico", "Plaza Mayor, San Francisco y barrios coloniales.", "Lima", "Lima", "Cultural", 95m, "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=800", 7),
            ("gastronomia-lima-barranco", "Tour Gastronómico Barranco", "Ceviche, pisco y restaurantes boutique.", "Lima", "Lima", "Gastronomico", 145m, "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800", 9),
            ("aventura-lunahuana-rafting", "Rafting Lunahuana Aventura", "Rafting clase II-III con almuerzo campestre.", "Lunahuana", "Lima", "Aventura", 175m, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800", 11),
            ("machu-picchu-full-day", "Machu Picchu Full Day desde Cusco", "Visita guiada a la ciudadela con tren o bus.", "Machu Picchu", "Cusco", "FullDay", 420m, "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=800", 14),
            ("city-tour-cusco-sagrado", "City Tour Cusco + Valle Sagrado", "Sacsayhuaman, Pisac y mercado artesanal.", "Cusco", "Cusco", "Cultural", 220m, "https://images.unsplash.com/photo-1537996194471-e657b7758544?w=800", 16),
            ("trekking-rainbow-mountain", "Trekking Montaña de Colores", "Vinicunca con desayuno y oxígeno incluido.", "Cusco", "Cusco", "Trekking", 165m, "https://images.unsplash.com/photo-1519681393784-d120267933ba?w=800", 12),
            ("aventura-sacred-valley-atv", "Valle Sagrado en ATV", "Moray, Maras y salineras en cuatrimoto.", "Urubamba", "Cusco", "Aventura", 198m, "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800", 18),
            ("colca-canyon-condor", "Cañón del Colca y Cóndor", "Mirador del cóndor y pueblos del valle.", "Chivay", "Arequipa", "FullDay", 185m, "https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=800", 10),
            ("city-tour-arequipa", "City Tour Arequipa Blanca", "Monasterio Santa Catalina y centro histórico.", "Arequipa", "Arequipa", "Cultural", 110m, "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=800", 8),
            ("trekking-misti-base", "Trekking Base del Misti", "Ascenso moderado con vista a la ciudad.", "Arequipa", "Arequipa", "Trekking", 240m, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800", 20),
            ("lago-titicaca-uros", "Lago Titicaca Islas Uros y Taquile", "Paseo en totora y comunidad local.", "Puno", "Puno", "Cultural", 175m, "https://images.unsplash.com/photo-1476514525535-07fb3b4eae35?w=800", 13),
            ("full-day-sillustani", "Sillustani y Atardecer Lago", "Chullpas preincas y mirador del lago.", "Puno", "Puno", "FullDay", 130m, "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=800", 15),
            ("amazonia-loreto-3d2n", "Selva Loreto 3D/2N", "Cabaña, caminata y avistamiento de fauna.", "Iquitos", "Loreto", "Aventura", 890m, "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=800", 22),
            ("full-day-iquitos-belen", "Belén y Río Amazonas", "Mercado flotante y navegación por el río.", "Iquitos", "Loreto", "FullDay", 320m, "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800", 17),
            ("ica-ruta-vinos", "Ruta del Vino y Pisco Ica", "Bodegas, cata y maridaje en el sur.", "Ica", "Ica", "Gastronomico", 155m, "https://images.unsplash.com/photo-1551632811-561732d1e306?w=800", 6),
            ("huacachina-sandboard-sunset", "Huacachina Sandboard Sunset", "Sandboard y paseo en tubulares al atardecer.", "Huacachina", "Ica", "Aventura", 165m, "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 4),
            ("nazca-lineas-aereas", "Sobrevuelo Líneas de Nazca", "Vuelo panorámico con briefing arqueológico.", "Nazca", "Ica", "FullDay", 450m, "https://images.unsplash.com/photo-1501785888041-af3ef285b470?w=800", 19)
        };

        var tours = new List<Tour>();
        for (var i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            if (existingSlugs.Contains(d.Slug, StringComparer.OrdinalIgnoreCase))
                continue;

            tours.Add(BuildTour(Pick(i).Id, d.Slug, d.Title, d.Desc, d.Loc, d.Dept, d.Type, d.Price, d.Img, d.Days));
        }

        return tours;
    }

    private static Tour BuildTour(int partnerId, string slug, string title, string description,
        string location, string department, string adventureType, decimal price, string? imageUrl, int daysAhead) =>
        new()
        {
            PartnerId = partnerId,
            Slug = slug,
            Title = title,
            Description = description,
            MetaTitle = $"{title} | DestinoPerú",
            MetaDescription = description.Length > 155 ? description[..155] : description,
            Price = price,
            Location = location,
            Department = department,
            AdventureType = adventureType,
            Date = DateTime.UtcNow.AddDays(daysAhead),
            Capacity = 28,
            AvailableCapacity = 28,
            ImageUrl = imageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
}
