using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DestinoPeruAPI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace DestinoPeruAPI.Infrastructure;
public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(int userId, string email, string role, string name, int? partnerId = null, bool impersonating = false)
    {
        var jwt = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, role),
            new("name", name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (partnerId.HasValue)
            claims.Add(new Claim("partner_id", partnerId.Value.ToString()));
        if (impersonating)
            claims.Add(new Claim("impersonating", "true"));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"], audience: jwt["Audience"],
            claims: claims, expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}