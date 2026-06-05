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

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await EnsurePartnerAdminRolesAsync(db, logger);
        await EnsureUserIfMissingAsync(db, SuperAdminEmail, "Super Admin DestinoPerú", RoleNames.SuperAdmin, SystemPassword, logger);
        await EnsureUserIfMissingAsync(db, DemoClienteEmail, "Usuario Demo", RoleNames.Cliente, "Demo123!", logger);
        await EnsureUserIfMissingAsync(db, AgencyAdminEmail, "Admin Agencia Demo", RoleNames.Admin, SystemPassword, logger);
        logger.LogInformation("Seed: cuentas de sistema verificadas (sin agencias ni tours demo).");
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
