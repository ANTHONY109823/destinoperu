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

        logger.LogInformation("Sembrando datos de demostracion (2 partners, 4 tours)...");

        var partnerUsers = new[]
        {
            CreateUser("Andes Explorer", "andes@destinoperu.demo", "Agencia"),
            CreateUser("Costa Peru Tours", "costa@destinoperu.demo", "Agencia")
        };
        db.Users.AddRange(partnerUsers);
        await db.SaveChangesAsync();

        var partners = new[]
        {
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
        };
        db.Partners.AddRange(partners);
        await db.SaveChangesAsync();

        var tours = new List<Tour>
        {
            BuildTour(partners[0].Id, "full-day-paracas-huacachina", "Full Day Paracas e Huacachina",
                "Navega por la Reserva de Paracas y disfruta del oasis de Huacachina.", "Ica", "Ica", "FullDay", 189,
                "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 14),
            BuildTour(partners[0].Id, "city-tour-cusco-sagrado", "City Tour Cusco + Valle Sagrado",
                "Recorre el ombligo del mundo con guias certificados.", "Cusco", "Cusco", "Cultural", 220,
                "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=800", 21),
            BuildTour(partners[1].Id, "trekking-rainbow-mountain", "Trekking Montana de Colores",
                "Aventura de altura con desayuno y transporte incluido.", "Cusco", "Cusco", "Trekking", 165,
                "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800", 10),
            BuildTour(partners[1].Id, "gastronomia-lima-barranco", "Tour Gastronomico Barranco",
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
