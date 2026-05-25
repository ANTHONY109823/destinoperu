namespace DestinoPeruBlazor.Helpers;

public static class SuperAdminCategories
{
    public static readonly IReadOnlyList<SuperAdminCategory> All =
    [
        new("tours", "Tours", "🗺️", "/superadmin/tours", 0,
            "Agencias de experiencias, paquetes y full days."),
        new("hoteles", "Hoteles", "🏨", "/superadmin/hoteles", 1,
            "Alojamientos, lodges y hospedaje."),
        new("restaurantes", "Restaurantes", "🍽️", "/superadmin/restaurantes", 2,
            "Gastronomía, reservas y menús."),
        new("cafes-bar", "Cafés / Bar", "☕", "/superadmin/cafes-bar", 3,
            "Rutas café, bares y nightlife.")
    ];

    public static SuperAdminCategory? FromRoute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var p = path.TrimEnd('/').ToLowerInvariant();
        return All.FirstOrDefault(c => p.EndsWith(c.Route, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsInicioRoute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var p = path.TrimEnd('/').ToLowerInvariant();
        return p is "/superadmin" or "/superadmin/inicio";
    }
}

public record SuperAdminCategory(
    string Key, string Label, string Icon, string Route, int PartnerType, string Description);
