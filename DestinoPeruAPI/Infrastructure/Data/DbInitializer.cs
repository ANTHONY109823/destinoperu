using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DestinoPeruAPI.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Tours.AnyAsync())
        {
            logger.LogInformation("Base de datos ya contiene tours; seed omitido.");
            return;
        }

        if (!await db.Partners.AnyAsync())
            logger.LogInformation("Sembrando partners y tours de demostracion...");
        else
            logger.LogInformation("Sembrando tours de demostracion...");

        Partner[] partners;
        if (!await db.Partners.AnyAsync())
        {
            var partnerUsers = new[]
            {
                CreateUser("Andes Explorer", "andes@destinoperu.demo", "Agencia"),
                CreateUser("Costa Peru Tours", "costa@destinoperu.demo", "Agencia")
            };
            db.Users.AddRange(partnerUsers);
            await db.SaveChangesAsync();

            partners =
            [
                new Partner
                {
                    UserId = partnerUsers[0].Id,
                    Name = "Andes Explorer SAC",
                    RUC = "20111111111",
                    PartnerType = PartnerType.Agencia,
                    Status = "Approved",
                    VerificationStatus = "Verified",
                    CommissionRate = 0.10m,
                    CreatedAt = DateTime.UtcNow
                },
                new Partner
                {
                    UserId = partnerUsers[1].Id,
                    Name = "Costa Peru Tours EIRL",
                    RUC = "20222222222",
                    PartnerType = PartnerType.Agencia,
                    Status = "Approved",
                    VerificationStatus = "Verified",
                    CommissionRate = 0.10m,
                    CreatedAt = DateTime.UtcNow
                }
            ];
            db.Partners.AddRange(partners);
            await db.SaveChangesAsync();
        }
        else
        {
            partners = await db.Partners.Where(p => p.Status == "Approved").Take(2).ToArrayAsync();
            if (partners.Length == 0)
                partners = await db.Partners.Take(2).ToArrayAsync();
            if (partners.Length == 0)
            {
                logger.LogWarning("No hay partners para asociar tours de prueba.");
                return;
            }
        }

        var p0 = partners[0];
        var p1 = partners.Length > 1 ? partners[1] : partners[0];

        var tours = new List<Tour>
        {
            BuildTour(p0.Id, "full-day-paracas-huacachina", "Full Day Paracas e Huacachina",
                "Navega por la Reserva de Paracas y disfruta del oasis de Huacachina.", "Ica", "Ica", "FullDay", 189,
                "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 14),
            BuildTour(p0.Id, "city-tour-cusco-sagrado", "City Tour Cusco + Valle Sagrado",
                "Recorre el ombligo del mundo con guias certificados.", "Cusco", "Cusco", "Cultural", 220,
                "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=800", 21),
            BuildTour(p1.Id, "trekking-rainbow-mountain", "Trekking Montana de Colores",
                "Aventura de altura con desayuno y transporte incluido.", "Cusco", "Cusco", "Trekking", 165,
                "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 10),
            BuildTour(p1.Id, "gastronomia-lima-barranco", "Tour Gastronomico Barranco",
                "Degusta la mejor cocina peruana frente al mar.", "Lima", "Lima", "Gastronomico", 145,
                "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=800", 7)
        };

        db.Tours.AddRange(tours);
        await db.SaveChangesAsync();
        logger.LogInformation("Seed completado: {Partners} partners, {Tours} tours.", partners.Length, tours.Count);
    }

    private static User CreateUser(string name, string email, string role) => new()
    {
        Name = name,
        Email = email,
        Role = role,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!", 12),
        CreatedAt = DateTime.UtcNow
    };

    private static Tour BuildTour(int partnerId, string slug, string title, string description,
        string location, string department, string adventureType, decimal price, string? imageUrl, int daysAhead) =>
        new()
        {
            PartnerId = partnerId,
            Slug = slug,
            Title = title,
            Description = description,
            MetaTitle = $"{title} | Destino Peru",
            MetaDescription = description.Length > 155 ? description[..155] : description,
            Price = price,
            Location = location,
            Department = department,
            AdventureType = adventureType,
            Date = DateTime.UtcNow.AddDays(daysAhead),
            Capacity = 24,
            AvailableCapacity = 24,
            ImageUrl = imageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
}
