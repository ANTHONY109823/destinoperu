using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DestinoPeruAPI.Application.Common;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(raw) || !int.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("Usuario no identificado en el token.");
        return id;
    }

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value
        ?? user.FindFirst("role")?.Value
        ?? "";

    public static string? GetClaim(this ClaimsPrincipal user, string type) =>
        user.FindFirst(type)?.Value;
}
