using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace DestinoPeruAPI.API.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    { var r = await authService.RegisterAsync(request); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    { var r = await authService.LoginAsync(request); return r.Success ? Ok(r) : Unauthorized(r); }
}