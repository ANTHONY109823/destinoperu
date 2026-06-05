using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DestinoPeruAPI.Infrastructure.Data;

/// <summary>
/// Seed mínimo: solo cuentas de sistema. No crea agencias demo ni tours automáticos.
/// La agencia Terruñito (u otras) deben existir por registro manual en producción.
/// </summary>
public static class DbInitializer
{
    private const string DemoClienteEmail = "demo@destinoperu.com";
    private const string SuperAdminEmail = "superadmin@destinoperu.com";
    private const string AgencyAdminEmail = "admin@destinoperu.com";
    private const string SystemPassword = "Admin2026!";

    private static readonly (string Name, string Image)[] DefaultDestinations =
    [
        ("Cusco", "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=600&q=80"),
        ("Ica", "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=600&q=80"),
        ("Arequipa", "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=600&q=80"),
        ("Lima", "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=600&q=80"),
        ("Puno", "https://images.unsplash.com/photo-1476514525535-07fb3b4eae35?w=600&q=80")
    ];

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await EnsurePartnerAdminRolesAsync(db, logger);
        await EnsureUserIfMissingAsync(db, SuperAdminEmail, "Super Admin DestinoPerú", RoleNames.SuperAdmin, SystemPassword, logger);
        await EnsureUserIfMissingAsync(db, DemoClienteEmail, "Usuario Demo", RoleNames.Cliente, "Demo123!", logger);
        await EnsureUserIfMissingAsync(db, AgencyAdminEmail, "Admin Agencia Demo", RoleNames.Admin, SystemPassword, logger);
        await EnsurePopularDestinationsAsync(db, logger);
        logger.LogInformation("Seed: cuentas de sistema verificadas (sin agencias ni tours demo).");
    }

    private static async Task EnsurePopularDestinationsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.PopularDestinations.AnyAsync()) return;

        var order = 1;
        foreach (var (name, image) in DefaultDestinations)
        {
            db.PopularDestinations.Add(new PopularDestination
            {
                Name = name,
                ImageUrl = image,
                Department = name,
                DisplayOrder = order++,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seed: {Count} destinos populares por defecto creados.", DefaultDestinations.Length);
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
}
