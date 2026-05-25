namespace DestinoPeruBlazor.Helpers;

public static class SuperAdminCategories
{
    public const string Tours = "tours";
    public const string Hoteles = "hoteles";
    public const string Restaurantes = "restaurantes";
    public const string CafesBar = "cafes-bar";

    public static readonly IReadOnlyList<SuperAdminCategory> All =
    [
        new(Tours, "Tours", "🗺️", "/superadmin/tours", 0,
            "Agencias de experiencias, paquetes y full days.",
            "Gestiona agencias de tours, publicaciones y cupos.",
            "Tours: crear experiencias, manifiestos y reservas por agencia."),
        new(Hoteles, "Hoteles", "🏨", "/superadmin/hoteles", 1,
            "Alojamiento y hospedaje.",
            "Socios hoteleros: habitaciones y tarifas (perfil hotel).",
            "Hoteles: socios tipo alojamiento — sin mezclar con tours."),
        new(Restaurantes, "Restaurantes", "🍽️", "/superadmin/restaurantes", 2,
            "Gastronomía y reservas de mesa.",
            "Restaurantes asociados: menús y reservas.",
            "Restaurantes: solo socios gastronómicos."),
        new(CafesBar, "Cafés / Bar", "☕", "/superadmin/cafes-bar", 3,
            "Rutas café, bares y nightlife.",
            "Locales café/bar: rutas y cupos (tazas).",
            "Cafés/Bar: experiencias café y bar nocturno.")
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
    string Key,
    string Label,
    string Icon,
    string Route,
    int PartnerType,
    string Description,
    string AdminHint,
    string ConfigNote);
