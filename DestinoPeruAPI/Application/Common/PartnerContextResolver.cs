using System.Security.Claims;
using DestinoPeruAPI.Application.Services;
using Microsoft.AspNetCore.Http;

namespace DestinoPeruAPI.Application.Common;

public static class PartnerContextResolver
{
    /// <summary>
    /// Solo SuperAdmin puede usar X-Partner-Id. Admin/Vendedor usan siempre el partner del JWT.
    /// </summary>
    public static async Task<int?> ResolvePartnerIdAsync(
        HttpRequest request,
        ClaimsPrincipal user,
        AgencyAdminService agencyService)
    {
        var role = user.GetRole();
        var userId = user.GetUserId();

        if (role == RoleNames.SuperAdmin)
        {
            if (request.Headers.TryGetValue("X-Partner-Id", out var headerVal) &&
                int.TryParse(headerVal.FirstOrDefault(), out var fromHeader))
                return fromHeader;

            if (int.TryParse(user.GetClaim("partner_id"), out var fromClaim))
                return fromClaim;

            return await agencyService.ResolvePartnerIdAsync(userId, role);
        }

        if (int.TryParse(user.GetClaim("partner_id"), out var partnerFromJwt))
            return partnerFromJwt;

        return await agencyService.ResolvePartnerIdAsync(userId, role);
    }
}
