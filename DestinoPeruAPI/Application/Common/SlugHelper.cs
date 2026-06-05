using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DestinoPeruAPI.Application.Common;

public static class SlugHelper
{
    public static string Generate(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "tour";
        var normalized = title.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        var slug = Regex.Replace(sb.ToString(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "tour" : slug;
    }

    /// <summary>Genera un slug único respecto a los ya existentes (insensible a mayúsculas).</summary>
    public static string GenerateUnique(string title, IEnumerable<string?> existing)
    {
        var baseSlug = Generate(title);
        var taken = new HashSet<string>(
            existing.Where(s => !string.IsNullOrWhiteSpace(s))!,
            StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(baseSlug)) return baseSlug;
        var i = 2;
        while (taken.Contains($"{baseSlug}-{i}")) i++;
        return $"{baseSlug}-{i}";
    }
}
