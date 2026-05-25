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

    /// <summary>Resuelve categoría desde URI absoluta, relativa o path (ignora ?query y #hash).</summary>
    public static SuperAdminCategory? FromRoute(string? uriOrPath)
    {
        var path = NormalizePath(uriOrPath);
        if (path is null) return null;
        return All.FirstOrDefault(c => path == c.Route);
    }

    public static SuperAdminCategory? FromNavigation(Microsoft.AspNetCore.Components.NavigationManager nav) =>
        FromRoute(nav.ToBaseRelativePath(nav.Uri));

    public static bool IsInicioRoute(string? uriOrPath)
    {
        var path = NormalizePath(uriOrPath);
        return path is "/superadmin" or "/superadmin/inicio";
    }

    private static string? NormalizePath(string? uriOrPath)
    {
        if (string.IsNullOrWhiteSpace(uriOrPath)) return null;

        var raw = uriOrPath;
        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var absolute))
            raw = absolute.AbsolutePath;

        var noQuery = raw.Split('?', '#')[0].Trim();
        if (string.IsNullOrEmpty(noQuery)) return null;

        var path = noQuery.StartsWith('/') ? noQuery : "/" + noQuery;
        return path.TrimEnd('/').ToLowerInvariant();
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
