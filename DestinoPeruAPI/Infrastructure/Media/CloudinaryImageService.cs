using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DestinoPeruAPI.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DestinoPeruAPI.Infrastructure.Media;

public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;

    public CloudinaryImageService(IOptions<CloudinarySettings> options)
    {
        _settings = options.Value;
        var account = new Account(_settings.CloudName, _settings.ApiKey, _settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, string folder, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName))
            throw new InvalidOperationException("CloudinarySettings no configurado.");

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = $"destinoperu/{folder}",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);
        if (result.Error != null)
            throw new InvalidOperationException(result.Error.Message);
        return result.SecureUrl.ToString();
    }

    public string OptimizeUrl(string? url, int width = 800)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
            return url ?? "";
        var insert = $"/f_auto,q_auto,w_{width}";
        var idx = url.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return url;
        return url.Insert(idx + 8, insert.TrimStart('/') + "/");
    }
}
