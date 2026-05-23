using DestinoPeruAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DestinoPeruAPI.API.Controllers;

[ApiController][Route("api/media")][Authorize]
public class MediaController(IImageService imageService) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "tours")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo requerido." });

        await using var stream = file.OpenReadStream();
        var url = await imageService.UploadImageAsync(stream, file.FileName, folder);
        var optimized = imageService.OptimizeUrl(url);
        return Ok(new { url = optimized, original = url });
    }
}
