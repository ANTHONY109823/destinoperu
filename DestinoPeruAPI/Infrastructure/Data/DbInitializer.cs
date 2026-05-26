using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DestinoPeruAPI.Infrastructure.Data;

public static class DbInitializer
{
    private const string DemoClienteEmail = "demo@destinoperu.com";
    private const string SuperAdminEmail = "superadmin@destinoperu.com";
    private const string AgencyAdminEmail = "admin@destinoperu.com";
    private const string SystemPassword = "Admin2026!";
    private const string PresentationPassword = "Demo2026!";
    private const int MinPresentationTours = 18;

    private static readonly (string Email, string AdminName, string AgencyName, string City, string Dept, string Ruc, string Logo, string Phone, string Desc)[]
        PresentationAgencies =
        [
            ("andes@destinoperu.com", "María Andes", "Cusco Andes Travel",
                "Cusco", "Cusco", "20111111111",
                "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=400",
                "+51 984 111 001", "Especialistas en Machu Picchu, Valle Sagrado y trekking."),
            ("costa@destinoperu.com", "Luis Costa", "Costa Verde Aventuras",
                "Lima", "Lima", "20122222222",
                "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=400",
                "+51 987 222 002", "City tours, gastronomía y aventura en la costa central."),
            ("colca@destinoperu.com", "Ana Colca", "Arequipa Volcano Tours",
                "Arequipa", "Arequipa", "20133333333",
                "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=400",
                "+51 954 333 003", "Cañón del Colca, city tour y trekking al Misti."),
            ("dunas@destinoperu.com", "Carlos Dunas", "Ica Dunas y Vinos",
                "Ica", "Ica", "20144444444",
                "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=400",
                "+51 956 444 004", "Huacachina, viñedos y líneas de Nazca."),
            ("titicaca@destinoperu.com", "Rosa Titicaca", "Puno Lago Azul Experience",
                "Puno", "Puno", "20155555555",
                "https://images.unsplash.com/photo-1476514525535-07fb3b4eae35?w=400",
                "+51 951 555 005", "Islas Uros, Taquile y cultura altiplánica."),
            ("selva@destinoperu.com", "Pedro Selva", "Loreto Amazonía Verde",
                "Iquitos", "Loreto", "20166666666",
                "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=400",
                "+51 965 666 006", "Expediciones de selva y río Amazonas.")
        ];

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await EnsurePartnerAdminRolesAsync(db, logger);
        await EnsureUserIfMissingAsync(db, SuperAdminEmail, "Super Admin DestinoPerú", RoleNames.SuperAdmin, SystemPassword, logger);
        await EnsureUserIfMissingAsync(db, DemoClienteEmail, "Usuario Demo", RoleNames.Cliente, "Demo123!", logger);

        var adminUser = await EnsureUserIfMissingAsync(db, AgencyAdminEmail, "Admin Agencia Demo", RoleNames.Admin, SystemPassword, logger);
        await EnsureAdminDemoPartnerAsync(db, adminUser, logger);

        var presentationPartners = await EnsurePresentationAgenciesAsync(db, logger);

        var tourCount = await db.Tours.CountAsync();
        if (tourCount < MinPresentationTours && presentationPartners.Count > 0)
        {
            var slugs = await db.Tours.Select(t => t.Slug).ToListAsync();
            var tours = BuildPresentationTours(presentationPartners, slugs);
            if (tours.Count > 0)
            {
                db.Tours.AddRange(tours);
                await db.SaveChangesAsync();
                logger.LogInformation("Seed: +{Count} tours de presentación.", tours.Count);
            }
        }

        await EnsureToursPerPresentationAgencyAsync(db, presentationPartners, logger);
        await AssignOrphanToursAsync(db, logger);
        await RedistributeToursByDepartmentAsync(db, logger);
    }

    private static async Task EnsurePartnerAdminRolesAsync(AppDbContext db, ILogger logger)
    {
        var legacy = await db.Users.Where(u => u.Role == "Agencia").ToListAsync();
        foreach (var u in legacy)
        {
            u.Role = RoleNames.Admin;
            logger.LogInformation("Rol Agencia→Admin: {Email}", u.Email);
        }
        if (legacy.Count > 0) await db.SaveChangesAsync();
    }

    private static async Task<User> EnsureUserIfMissingAsync(
        AppDbContext db, string email, string name, string role, string password, ILogger? logger)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is not null) return user;

        user = new User
        {
            Name = name,
            Email = email,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger?.LogInformation("Usuario creado (solo si no existía): {Email} ({Role})", email, role);
        return user;
    }

    private static async Task EnsureAdminDemoPartnerAsync(AppDbContext db, User adminUser, ILogger logger)
    {
        var partner = await db.Partners.FirstOrDefaultAsync(p => p.UserId == adminUser.Id);
        if (partner is null)
        {
            partner = new Partner
            {
                UserId = adminUser.Id,
                Name = "Agencia Demo DestinoPerú",
                RUC = "20999999991",
                PartnerType = PartnerType.Agencia,
                Status = "Approved",
                VerificationStatus = "Verified",
                OperatingDepartment = "Lima",
                ContactEmail = AgencyAdminEmail,
                ContactPhone = "+51 999 000 099",
                LogoUrl = "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=400",
                CommissionRate = 0.10m,
                CreatedAt = DateTime.UtcNow
            };
            db.Partners.Add(partner);
            await db.SaveChangesAsync();
            logger.LogInformation("Partner admin@ creado.");
            return;
        }

        await EnsureAgencyToursIfFewAsync(db, partner.Id, "demo-agencia-", 3, logger);
    }

    private static async Task<List<Partner>> EnsurePresentationAgenciesAsync(AppDbContext db, ILogger logger)
    {
        var list = new List<Partner>();
        foreach (var s in PresentationAgencies)
        {
            var user = await EnsureUserIfMissingAsync(db, s.Email, s.AdminName, RoleNames.Admin, PresentationPassword, logger);

            var partner = await db.Partners.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (partner is null)
            {
                partner = new Partner
                {
                    UserId = user.Id,
                    Name = s.AgencyName,
                    RUC = s.Ruc,
                    PartnerType = PartnerType.Agencia,
                    Status = "Approved",
                    VerificationStatus = "Verified",
                    OperatingDepartment = s.Dept,
                    ContactEmail = s.Email,
                    ContactPhone = s.Phone,
                    LogoUrl = s.Logo,
                    CommissionRate = 0.10m,
                    CreatedAt = DateTime.UtcNow
                };
                db.Partners.Add(partner);
                await db.SaveChangesAsync();
                logger.LogInformation("Agencia presentación creada: {Name}", s.AgencyName);
            }

            list.Add(partner);
        }

        await EnsureCategoryShowcaseAsync(db);
        return list;
    }

    private static async Task EnsureCategoryShowcaseAsync(AppDbContext db)
    {
        var showcases = new (string Email, string Name, string Ruc, string Dept, PartnerType Type)[]
        {
            ("hotel.costa@destinoperu.com", "Hotel Costa Verde Miraflores", "20100020001", "Lima", PartnerType.Hotel),
            ("rest.sabor@destinoperu.com", "Restaurante Sabor Andino", "20100020002", "Cusco", PartnerType.Restaurante),
            ("cafe.barranco@destinoperu.com", "Café Bar Barranco Lounge", "20100020003", "Lima", PartnerType.CafeBar)
        };

        foreach (var s in showcases)
        {
            if (await db.Partners.AnyAsync(p => p.RUC == s.Ruc)) continue;
            var user = await EnsureUserIfMissingAsync(db, s.Email, s.Name, RoleNames.Admin, PresentationPassword, logger: null);
            db.Partners.Add(new Partner
            {
                UserId = user.Id,
                Name = s.Name,
                RUC = s.Ruc,
                PartnerType = s.Type,
                Status = "Approved",
                VerificationStatus = "Verified",
                OperatingDepartment = s.Dept,
                ContactEmail = s.Email,
                LogoUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=400",
                CommissionRate = 0.10m,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureToursPerPresentationAgencyAsync(AppDbContext db, List<Partner> partners, ILogger logger)
    {
        var slugs = await db.Tours.Select(t => t.Slug).ToListAsync();
        foreach (var p in partners)
        {
            var count = await db.Tours.CountAsync(t => t.PartnerId == p.Id);
            if (count >= 2) continue;

            var dept = p.OperatingDepartment ?? "Lima";
            var extras = new (string Slug, string Title, string Type, decimal Price)[]
            {
                ($"demo-{p.Id}-fd1", $"Full Day {dept}", "FullDay", 120m + p.Id),
                ($"demo-{p.Id}-cul1", $"Tour Cultural {dept}", "Cultural", 95m + p.Id)
            };

            foreach (var e in extras)
            {
                if (slugs.Contains(e.Slug, StringComparer.OrdinalIgnoreCase)) continue;
                db.Tours.Add(BuildTour(p.Id, e.Slug, e.Title, $"Experiencia demo en {dept}.", dept, dept, e.Type, e.Price,
                    "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 7 + p.Id));
                slugs.Add(e.Slug);
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAgencyToursIfFewAsync(AppDbContext db, int partnerId, string slugPrefix, int min, ILogger logger)
    {
        var existing = await db.Tours.CountAsync(t => t.PartnerId == partnerId);
        if (existing >= min) return;
        var slugs = await db.Tours.Select(t => t.Slug).ToListAsync();
        for (var i = existing; i < min; i++)
        {
            var slug = $"{slugPrefix}{i + 1}";
            if (slugs.Contains(slug)) continue;
            db.Tours.Add(BuildTour(partnerId, slug, $"Tour demo admin #{i + 1}", "Tour de demostración.",
                "Lima", "Lima", "FullDay", 99m + i,
                "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 5 + i));
        }
        await db.SaveChangesAsync();
    }

    private static async Task AssignOrphanToursAsync(AppDbContext db, ILogger logger)
    {
        var agencyIds = await db.Partners
            .Where(p => p.PartnerType == PartnerType.Agencia)
            .Select(p => p.Id)
            .ToListAsync();
        if (agencyIds.Count == 0) return;

        var orphans = await db.Tours
            .Where(t => !agencyIds.Contains(t.PartnerId))
            .ToListAsync();
        if (orphans.Count == 0) return;

        for (var i = 0; i < orphans.Count; i++)
        {
            orphans[i].PartnerId = agencyIds[i % agencyIds.Count];
            logger.LogInformation("Tour huérfano {Id} → partner {PartnerId}", orphans[i].Id, orphans[i].PartnerId);
        }
        await db.SaveChangesAsync();
    }

    private static async Task RedistributeToursByDepartmentAsync(AppDbContext db, ILogger logger)
    {
        var presentationEmails = PresentationAgencies.Select(a => a.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var partners = await db.Partners
            .Include(p => p.User)
            .Where(p => p.PartnerType == PartnerType.Agencia && presentationEmails.Contains(p.User!.Email))
            .ToListAsync();
        if (partners.Count == 0) return;

        var legacyPartnerIds = await db.Partners
            .Include(p => p.User)
            .Where(p => p.User!.Email.Contains("@demo.dp"))
            .Select(p => p.Id)
            .ToListAsync();

        if (legacyPartnerIds.Count == 0) return;

        var byDept = partners
            .GroupBy(p => p.OperatingDepartment ?? "", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var tours = await db.Tours.Where(t => legacyPartnerIds.Contains(t.PartnerId)).ToListAsync();
        foreach (var tour in tours)
        {
            if (!byDept.TryGetValue(tour.Department ?? "", out var candidates) || candidates.Count == 0)
                candidates = partners;
            tour.PartnerId = candidates[tour.Id % candidates.Count].Id;
        }

        if (tours.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Tours legacy reasignados a agencias presentación: {Count}", tours.Count);
        }
    }

    private static List<Tour> BuildPresentationTours(List<Partner> partners, List<string> existingSlugs)
    {
        Partner Pick(int i) => partners[i % partners.Count];
        Partner PickDept(string dept) =>
            partners.FirstOrDefault(p => string.Equals(p.OperatingDepartment, dept, StringComparison.OrdinalIgnoreCase))
            ?? Pick(0);

        var defs = new (string Slug, string Title, string Desc, string Loc, string Dept, string Type, decimal Price, string Img, int Days)[]
        {
            ("full-day-paracas-huacachina", "Full Day Paracas e Huacachina", "Islas Ballestas y oasis.", "Paracas", "Ica", "FullDay", 189m, "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 5),
            ("city-tour-lima-centro", "City Tour Lima Centro Histórico", "Plaza Mayor y San Francisco.", "Lima", "Lima", "Cultural", 95m, "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=800", 7),
            ("machu-picchu-full-day", "Machu Picchu Full Day", "Ciudadela con guía.", "Machu Picchu", "Cusco", "FullDay", 420m, "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=800", 14),
            ("city-tour-cusco-sagrado", "City Tour Cusco + Valle Sagrado", "Sacsayhuaman y Pisac.", "Cusco", "Cusco", "Cultural", 220m, "https://images.unsplash.com/photo-1537996194471-e657b7758544?w=800", 16),
            ("trekking-rainbow-mountain", "Montaña de Colores", "Vinicunca full day.", "Cusco", "Cusco", "Trekking", 165m, "https://images.unsplash.com/photo-1519681393784-d120267933ba?w=800", 12),
            ("colca-canyon-condor", "Cañón del Colca", "Mirador del cóndor.", "Chivay", "Arequipa", "FullDay", 185m, "https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=800", 10),
            ("city-tour-arequipa", "City Tour Arequipa", "Centro histórico.", "Arequipa", "Arequipa", "Cultural", 110m, "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=800", 8),
            ("lago-titicaca-uros", "Lago Titicaca Uros", "Islas flotantes.", "Puno", "Puno", "Cultural", 175m, "https://images.unsplash.com/photo-1476514525535-07fb3b4eae35?w=800", 13),
            ("amazonia-loreto-3d2n", "Selva Loreto 3D/2N", "Cabaña y fauna.", "Iquitos", "Loreto", "Aventura", 890m, "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800", 22),
            ("ica-ruta-vinos", "Ruta del Vino Ica", "Cata y bodegas.", "Ica", "Ica", "Gastronomico", 155m, "https://images.unsplash.com/photo-1551632811-561732d1e306?w=800", 6),
            ("huacachina-sandboard", "Huacachina Sandboard", "Atardecer en dunas.", "Huacachina", "Ica", "Aventura", 165m, "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 4),
            ("gastronomia-lima-barranco", "Tour Gastronómico Barranco", "Ceviche y pisco.", "Lima", "Lima", "Gastronomico", 145m, "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800", 9),
            ("aventura-lunahuana", "Rafting Lunahuana", "Clase II-III.", "Lunahuana", "Lima", "Aventura", 175m, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800", 11),
            ("full-day-sillustani", "Sillustani", "Chullpas preincas.", "Puno", "Puno", "FullDay", 130m, "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=800", 15),
            ("trekking-misti-base", "Trekking Misti", "Ascenso moderado.", "Arequipa", "Arequipa", "Trekking", 240m, "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800", 20),
            ("nazca-lineas-aereas", "Líneas de Nazca", "Sobrevuelo.", "Nazca", "Ica", "FullDay", 450m, "https://images.unsplash.com/photo-1501785888041-af3ef285b470?w=800", 19),
            ("aventura-sacred-valley-atv", "Valle Sagrado ATV", "Moray y Maras.", "Urubamba", "Cusco", "Aventura", 198m, "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800", 18),
            ("full-day-iquitos-belen", "Belén Amazonas", "Mercado y río.", "Iquitos", "Loreto", "FullDay", 320m, "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800", 17)
        };

        var tours = new List<Tour>();
        for (var i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            if (existingSlugs.Contains(d.Slug, StringComparer.OrdinalIgnoreCase)) continue;
            var partner = PickDept(d.Dept);
            tours.Add(BuildTour(partner.Id, d.Slug, d.Title, d.Desc, d.Loc, d.Dept, d.Type, d.Price, d.Img, d.Days));
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
