using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Domain.Entities;
using DestinoPeruAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DestinoPeruAPI.Application.Services;

public class PopularDestinationService(AppDbContext db)
{
    public async Task<IReadOnlyList<PopularDestinationDto>> GetPublicAsync()
    {
        var items = await db.PopularDestinations
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id)
            .ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PopularDestinationDto>> GetAllAsync()
    {
        var items = await db.PopularDestinations
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id)
            .ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<ApiResponse<PopularDestinationDto>> CreateAsync(UpsertPopularDestinationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return new ApiResponse<PopularDestinationDto>(false, "El nombre es obligatorio.", null);

        var nextOrder = request.DisplayOrder > 0
            ? request.DisplayOrder
            : (await db.PopularDestinations.MaxAsync(d => (int?)d.DisplayOrder) ?? 0) + 1;

        var entity = new PopularDestination
        {
            Name = request.Name.Trim(),
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            Department = string.IsNullOrWhiteSpace(request.Department) ? request.Name.Trim() : request.Department.Trim(),
            DisplayOrder = nextOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        db.PopularDestinations.Add(entity);
        await db.SaveChangesAsync();
        return new ApiResponse<PopularDestinationDto>(true, "Destino creado.", Map(entity));
    }

    public async Task<ApiResponse<PopularDestinationDto>> UpdateAsync(int id, UpsertPopularDestinationRequest request)
    {
        var entity = await db.PopularDestinations.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null) return new ApiResponse<PopularDestinationDto>(false, "Destino no encontrado.", null);
        if (string.IsNullOrWhiteSpace(request.Name))
            return new ApiResponse<PopularDestinationDto>(false, "El nombre es obligatorio.", null);

        entity.Name = request.Name.Trim();
        entity.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        entity.Department = string.IsNullOrWhiteSpace(request.Department) ? request.Name.Trim() : request.Department.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return new ApiResponse<PopularDestinationDto>(true, "Destino actualizado.", Map(entity));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var entity = await db.PopularDestinations.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null) return new ApiResponse<bool>(false, "Destino no encontrado.", false);
        db.PopularDestinations.Remove(entity);
        await db.SaveChangesAsync();
        return new ApiResponse<bool>(true, "Destino eliminado.", true);
    }

    public async Task<ApiResponse<bool>> MoveAsync(int id, int direction)
    {
        var ordered = await db.PopularDestinations
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id).ToListAsync();
        var index = ordered.FindIndex(d => d.Id == id);
        if (index < 0) return new ApiResponse<bool>(false, "Destino no encontrado.", false);

        var swapIndex = direction < 0 ? index - 1 : index + 1;
        if (swapIndex < 0 || swapIndex >= ordered.Count)
            return new ApiResponse<bool>(true, "Sin cambios.", true);

        (ordered[index].DisplayOrder, ordered[swapIndex].DisplayOrder) =
            (ordered[swapIndex].DisplayOrder, ordered[index].DisplayOrder);
        await db.SaveChangesAsync();
        return new ApiResponse<bool>(true, "Orden actualizado.", true);
    }

    private static PopularDestinationDto Map(PopularDestination d) =>
        new(d.Id, d.Name, d.ImageUrl, d.Department, d.DisplayOrder, d.IsActive);
}
