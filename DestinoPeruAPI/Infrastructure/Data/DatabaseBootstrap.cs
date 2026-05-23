using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DestinoPeruAPI.Infrastructure.Data;

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(AppDbContext db, ILogger logger)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
            logger.LogInformation("Migraciones pendientes ({Count}): {Names}", pending.Count, string.Join(", ", pending));

        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrate() completado.");
        }
        catch (PostgresException ex) when (ex.SqlState is "23503" or "42P01")
        {
            logger.LogWarning(ex, "Error de esquema al migrar; intentando reparar FK Partners...");
            await DropPartnerFkIfExistsAsync(db);
            await db.Database.MigrateAsync();
        }

        await DropPartnerFkIfExistsAsync(db);
        await RepairOrphanToursAsync(db, logger);
        await DbInitializer.SeedAsync(db, logger);
        await TryRestorePartnerFkAsync(db, logger);
    }

    private static async Task DropPartnerFkIfExistsAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "Tours" DROP CONSTRAINT IF EXISTS "FK_Tours_Partners_PartnerId";""");
    }

    private static async Task TryRestorePartnerFkAsync(AppDbContext db, ILogger logger)
    {
        var orphanCount = await db.Tours
            .Where(t => !db.Partners.Any(p => p.Id == t.PartnerId))
            .CountAsync();
        if (orphanCount > 0)
        {
            logger.LogWarning("Hay {Count} tours sin partner; FK no restaurada.", orphanCount);
            return;
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Tours_Partners_PartnerId'
                    ) THEN
                        ALTER TABLE "Tours"
                        ADD CONSTRAINT "FK_Tours_Partners_PartnerId"
                        FOREIGN KEY ("PartnerId") REFERENCES "Partners" ("Id") ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo restaurar FK_Tours_Partners_PartnerId.");
        }
    }

    private static async Task RepairOrphanToursAsync(AppDbContext db, ILogger logger)
    {
        if (!await db.Database.CanConnectAsync())
            return;

        try
        {
            if (await db.Partners.AnyAsync())
                return;
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            logger.LogError("La tabla Partners no existe. Migrate() no se aplico en Railway.");
            return;
        }

        if (await db.Tours.AnyAsync())
        {
            var count = await db.Tours.CountAsync();
            logger.LogWarning("Eliminando {Count} tours sin partners para permitir seed limpio.", count);
            db.Tours.RemoveRange(await db.Tours.ToListAsync());
            await db.SaveChangesAsync();
        }
    }

}
