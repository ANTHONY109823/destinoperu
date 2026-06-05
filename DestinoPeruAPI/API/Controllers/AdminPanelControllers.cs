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

    [HttpGet("tours/compare")]
    public async Task<IActionResult> CompareTours([FromQuery] string? department) =>
        Ok(await superAdminService.GetCompareToursAsync(department));

    [HttpDelete("partners/{id:int}")]
    public async Task<IActionResult> DeletePartner(int id)
    {
        var r = await superAdminService.DeleteAgencyAsync(id);
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

}

[ApiController]
[Route("api/superadmin/destinations")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class SuperAdminDestinationsController(PopularDestinationService destinationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await destinationService.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPopularDestinationRequest request)
    {
        var r = await destinationService.CreateAsync(request);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertPopularDestinationRequest request)
    {
        var r = await destinationService.UpdateAsync(id, request);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await destinationService.DeleteAsync(id);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("{id:int}/move")]
    public async Task<IActionResult> Move(int id, [FromQuery] int direction)
    {
        var r = await destinationService.MoveAsync(id, direction);
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

    private Task<int?> ResolvePartnerIdAsync() =>
        PartnerContextResolver.ResolvePartnerIdAsync(Request, User, agencyService);

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetProfileAsync(partnerId.Value);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpPut("profile")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAgencyRequest request)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateProfileAsync(partnerId.Value, request, Role);
        return r.Success ? Ok(r.Data) : BadRequest(r);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetDashboardAsync(partnerId.Value);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpGet("tours")]
    public async Task<IActionResult> Tours()
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetToursAsync(partnerId.Value);
        return Ok(r.Data);
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> Reservations()
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetReservationsAsync(partnerId.Value);
        return Ok(r.Data);
    }

    [HttpGet("manifest")]
    public async Task<IActionResult> Manifest([FromQuery] int? tourId = null)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetManifestAsync(partnerId.Value, tourId);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpPut("reservations/{id:int}/status")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> SetReservationStatus(int id, [FromQuery] string status = "Confirmed")
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.SetReservationStatusAsync(id, partnerId.Value, status);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("vendors")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.CreateVendorAsync(partnerId.Value, request, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("tours")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.CreateTourAsync(request, UserId, Role, partnerId);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("tours/{tourId:int}/capacity")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> UpdateCapacity(int tourId, [FromQuery] int available)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateTourCapacityAsync(tourId, partnerId.Value, available, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("tours/{tourId:int}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Vendedor},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> UpdateTour(int tourId, [FromBody] UpdateTourItemRequest request)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateTourItemAsync(tourId, partnerId.Value, request, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpDelete("tours/{tourId:int}")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<IActionResult> DeleteTour(int tourId)
    {
        var partnerId = await ResolvePartnerIdAsync();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.DeleteTourAsync(tourId, partnerId.Value, UserId, Role);
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
