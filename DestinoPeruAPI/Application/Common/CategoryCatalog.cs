using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Application.Common;

public static class CategoryCatalog
{
    public const string Tours = "tours";
    public const string Hoteles = "hoteles";
    public const string Restaurantes = "restaurantes";
    public const string CafesBar = "cafes-bar";

    public static readonly IReadOnlyList<CategoryInfo> All =
    [
        new(Tours, "Tours", "🗺️", PartnerType.Agencia, "Experiencias y paquetes turísticos"),
        new(Hoteles, "Hoteles", "🏨", PartnerType.Hotel, "Alojamiento y hospedaje"),
        new(Restaurantes, "Restaurantes", "🍽️", PartnerType.Restaurante, "Gastronomía y reservas"),
        new(CafesBar, "Cafés / Bar", "☕", PartnerType.CafeBar, "Rutas café, bares y nightlife")
    ];

    public static CategoryInfo? FromKey(string? key) =>
        All.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    public static CategoryInfo FromPartnerType(PartnerType type) =>
        All.FirstOrDefault(c => c.PartnerType == type) ?? All[0];
}

public record CategoryInfo(string Key, string Label, string Icon, PartnerType PartnerType, string Description);
