using System.Globalization;
using System.Text;

namespace DestinoPeruAPI.Application.Common;

public static class PartnerNameNormalizer
{
    public const string TerrunitoCanonical = "terrunito";

    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
    }

    public static bool IsTerrunito(string? name) =>
        Normalize(name) == TerrunitoCanonical;
}
