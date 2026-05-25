using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DestinoPeruAPI.API.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class SuperAdminController(SuperAdminService superAdminService) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics() => Ok(await superAdminService.GetMetricsAsync());

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard() => Ok(await superAdminService.GetDashboardAsync());

    [HttpGet("ranking")]
    public async Task<IActionResult> GetRanking() => Ok(await superAdminService.GetRankingAsync());

    [HttpGet("partners")]
    public async Task<IActionResult> GetPartners() => Ok(await superAdminService.GetPartnersAsync());

    [HttpGet("partners/{partnerId:int}/staff")]
    public async Task<IActionResult> GetStaff(int partnerId) => Ok(await superAdminService.GetPartnerStaffAsync(partnerId));

    [HttpPost("partners")]
    public async Task<IActionResult> CreateAgency([FromBody] CreateAgencyRequest request)
    {
        var r = await superAdminService.CreateAgencyAsync(request);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("partners/{id:int}")]
    public async Task<IActionResult> UpdateAgency(int id, [FromBody] UpdateAgencyRequest request)
    {
        var r = await superAdminService.UpdateAgencyAsync(id, request);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("partners/{id:int}/suspend")]
    public async Task<IActionResult> Suspend(int id, [FromQuery] bool suspend = true)
    {
        var r = await superAdminService.SuspendPartnerAsync(id, suspend);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("impersonate/{userId:int}")]
    public async Task<IActionResult> Impersonate(int userId)
    {
        try
        {
            var r = await superAdminService.ImpersonateAsync(userId);
            return r.Success ? Ok(r) : BadRequest(r);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<AuthResponse>(false, ex.Message, null));
        }
    }

    [HttpPost("partners/{partnerId:int}/tours")]
    public async Task<IActionResult> CreateTourForPartner(int partnerId, [FromBody] CreateTourRequest request)
    {
        var r = await superAdminService.CreateTourForPartnerAsync(partnerId, request);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}

[ApiController]
[Route("api/agency")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.Agencia},{RoleNames.SuperAdmin}")]
public class AgencyController(AgencyAdminService agencyService) : ControllerBase
{
    private int UserId => User.GetUserId();
    private string Role => User.GetRole();

    private async Task<int?> PartnerIdOrBadRequest()
    {
        if (Request.Headers.TryGetValue("X-Partner-Id", out var headerVal) &&
            int.TryParse(headerVal.FirstOrDefault(), out var fromHeader))
            return fromHeader;

        if (int.TryParse(User.GetClaim("partner_id"), out var fromClaim))
            return fromClaim;

        return await agencyService.ResolvePartnerIdAsync(UserId, Role);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetProfileAsync(partnerId.Value);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpPut("profile")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAgencyRequest request)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateProfileAsync(partnerId.Value, request, Role);
        return r.Success ? Ok(r.Data) : BadRequest(r);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetDashboardAsync(partnerId.Value);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpGet("tours")]
    public async Task<IActionResult> Tours()
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetToursAsync(partnerId.Value);
        return Ok(r.Data);
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> Reservations()
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetReservationsAsync(partnerId.Value);
        return Ok(r.Data);
    }

    [HttpGet("manifest")]
    public async Task<IActionResult> Manifest([FromQuery] int? tourId = null)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetManifestAsync(partnerId.Value, tourId);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpPut("reservations/{id:int}/status")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> SetReservationStatus(int id, [FromQuery] string status = "Confirmed")
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.SetReservationStatusAsync(id, partnerId.Value, status);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("vendors")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.CreateVendorAsync(partnerId.Value, request, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("tours")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        int? headerPartner = Request.Headers.TryGetValue("X-Partner-Id", out var h) && int.TryParse(h.FirstOrDefault(), out var pid) ? pid : null;
        var r = await agencyService.CreateTourAsync(request, UserId, Role, headerPartner);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("tours/{tourId:int}/capacity")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> UpdateCapacity(int tourId, [FromQuery] int available)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateTourCapacityAsync(tourId, partnerId.Value, available, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("tours/{tourId:int}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> UpdateTour(int tourId, [FromBody] UpdateTourItemRequest request)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateTourItemAsync(tourId, partnerId.Value, request, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(UserAccountService userAccountService) : ControllerBase
{
    [HttpGet("me/loyalty")]
    public async Task<IActionResult> Loyalty()
    {
        var userId = User.GetUserId();
        var r = await userAccountService.GetLoyaltyAsync(userId);
        return Ok(r.Data);
    }
}
