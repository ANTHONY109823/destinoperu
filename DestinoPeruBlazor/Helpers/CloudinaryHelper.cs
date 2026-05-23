namespace DestinoPeruBlazor.Helpers;

public static class CloudinaryHelper
{
    public static string Optimize(string? url, int width = 800)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
            return url ?? "";
        const string transform = "f_auto,q_auto";
        var idx = url.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return url;
        return url.Insert(idx + 8, $"{transform},w_{width}/");
    }
}
