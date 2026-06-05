using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DestinoPeruAPI.Infrastructure.Data;

/// <summary>
/// Limpieza única en producción: conserva agencias cuyo nombre coincide con Terruñito (normalizado).
/// </summary>
public static class AgencyCleanupMaintenance
{
    public const string RunKey = "v4_preserve_terrunito_cleanup";

    public static async Task RunOnceAsync(AppDbContext db, ILogger logger)
    {
        if (await db.AppMaintenanceRuns.AnyAsync(r => r.Key == RunKey))
        {
            logger.LogInformation("Limpieza de agencias ({Key}) ya ejecutada; se omite.", RunKey);
            return;
        }

        var agencies = await db.Partners
            .Where(p => p.PartnerType == PartnerType.Agencia)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var preserve = agencies.Where(p => PartnerNameNormalizer.IsTerrunito(p.Name)).ToList();
        if (preserve.Count == 0)
        {
            logger.LogError(
                "No se encontro la agencia Terruñito, abortando limpieza por seguridad. " +
                "Agencias tipo Agencia en BD: {Count}. Nombres: {Names}",
                agencies.Count,
                string.Join(" | ", agencies.Select(a => a.Name)));
            return;
        }

        var toDelete = agencies.Where(p => !PartnerNameNormalizer.IsTerrunito(p.Name)).ToList();

        logger.LogWarning(
            "=== LIMPIEZA AGENCIAS (pre-delete) === Preservar {KeepCount}: [{KeepNames}]. " +
            "Eliminar {DelCount}: [{DelNames}]",
            preserve.Count,
            string.Join(", ", preserve.Select(p => $"{p.Name} (id={p.Id})")),
            toDelete.Count,
            string.Join(", ", toDelete.Select(p => $"{p.Name} (id={p.Id})")));

        var toursRemoved = 0;
        var partnersRemoved = 0;
        var partnersSkipped = 0;

        foreach (var partner in toDelete)
        {
            var tourIds = await db.Tours.Where(t => t.PartnerId == partner.Id).Select(t => t.Id).ToListAsync();
            var tourIdsWithReservations = await db.Reservations
                .Where(r => tourIds.Contains(r.TourId))
                .Select(r => r.TourId)
                .Distinct()
                .ToListAsync();

            var deletableTourIds = tourIds.Except(tourIdsWithReservations).ToList();
            if (deletableTourIds.Count > 0)
            {
                var tours = await db.Tours.Where(t => deletableTourIds.Contains(t.Id)).ToListAsync();
                db.Tours.RemoveRange(tours);
                await db.SaveChangesAsync();
                toursRemoved += tours.Count;
                logger.LogInformation("Partner {PartnerId} ({Name}): {Count} tours sin reservas eliminados.",
                    partner.Id, partner.Name, tours.Count);
            }

            var remainingTours = await db.Tours.CountAsync(t => t.PartnerId == partner.Id);
            if (remainingTours > 0)
            {
                partnersSkipped++;
                logger.LogWarning(
                    "Partner {PartnerId} ({Name}) NO eliminado: quedan {Count} tours con reservas asociadas.",
                    partner.Id, partner.Name, remainingTours);
                continue;
            }

            var docs = await db.PartnerDocuments.Where(d => d.PartnerId == partner.Id).ToListAsync();
            if (docs.Count > 0) db.PartnerDocuments.RemoveRange(docs);

            var staff = await db.PartnerStaff.Where(s => s.PartnerId == partner.Id).ToListAsync();
            if (staff.Count > 0) db.PartnerStaff.RemoveRange(staff);

            db.Partners.Remove(partner);
            await db.SaveChangesAsync();
            partnersRemoved++;
            logger.LogInformation("Partner eliminado: {Name} (id={Id})", partner.Name, partner.Id);
        }

        db.AppMaintenanceRuns.Add(new AppMaintenanceRun { Key = RunKey, ExecutedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        logger.LogWarning(
            "=== LIMPIEZA COMPLETADA === Tours eliminados: {Tours}. Agencias eliminadas: {Partners}. " +
            "Agencias omitidas (tours con reservas): {Skipped}. Preservadas Terruñito: {Preserved}",
            toursRemoved, partnersRemoved, partnersSkipped, preserve.Count);
    }
}
