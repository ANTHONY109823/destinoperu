namespace DestinoPeruBlazor.Helpers;

/// <summary>URLs de galería por tour — reemplazar cuando haya fotos reales en Cloudinary.</summary>
public static class TourGalleryHelper
{
    public static IReadOnlyList<string> GetGalleryUrls(string? mainImageUrl, string slug, IReadOnlyList<string>? galeria = null)
    {
        if (galeria is { Count: > 0 })
            return galeria.Where(u => !string.IsNullOrWhiteSpace(u)).Take(8).ToList();

        if (GalleryBySlug.TryGetValue(slug, out var urls))
            return urls;

        var main = string.IsNullOrWhiteSpace(mainImageUrl)
            ? "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=1200"
            : mainImageUrl;

        return
        [
            main,
            WithSize(main, 1200),
            "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=1200",
            "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=1200"
        ];
    }

    private static string WithSize(string url, int w) =>
        url.Contains('?') ? $"{url}&w={w}" : $"{url}?w={w}";

    private static readonly Dictionary<string, string[]> GalleryBySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["full-day-paracas-huacachina"] =
        [
            "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=1200",
            "https://images.unsplash.com/photo-1551632811-561732d1e306?w=1200",
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1200",
            "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=1200"
        ],
        ["city-tour-cusco-sagrado"] =
        [
            "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=1200",
            "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=1200",
            "https://images.unsplash.com/photo-1537996194471-e657b7758544?w=1200",
            "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=1200"
        ],
        ["trekking-rainbow-mountain"] =
        [
            "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=1200",
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1200",
            "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=1200",
            "https://images.unsplash.com/photo-1519681393784-d120267933ba?w=1200"
        ],
        ["machu-picchu-full-day"] =
        [
            "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=1200",
            "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=1200",
            "https://images.unsplash.com/photo-1537996194471-e657b7758544?w=1200",
            "https://images.unsplash.com/photo-1516026679272-898a54700f3f?w=1200"
        ],
        ["lago-titicaca-uros"] =
        [
            "https://images.unsplash.com/photo-1526392060635-9d601b837dd0?w=1200",
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1200",
            "https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=1200",
            "https://images.unsplash.com/photo-1476514525535-07fb3b4eae35?w=1200"
        ]
    };
}
