using System.Text;

namespace DestinoPeruBlazor.Helpers;

public static class CsvExportHelper
{
    public static string Escape(string? value)
    {
        var s = value ?? "";
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    public static string ToCsv(IEnumerable<string[]> rows, string? headerLine = null)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(headerLine))
            sb.AppendLine(headerLine);
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        return sb.ToString();
    }

    public static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        return name.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
