using System.Security.Claims;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DestinoPeruAPI.API.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController(SuperAdminService superAdminService) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics() => Ok(await superAdminService.GetMetricsAsync());

    [HttpGet("partners")]
    public async Task<IActionResult> GetPartners() => Ok(await superAdminService.GetPartnersAsync());

    [HttpPost("partners")]
    public async Task<IActionResult> CreateAgency([FromBody] CreateAgencyRequest request)
    {
        var r = await superAdminService.CreateAgencyAsync(request);
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
        var superId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await superAdminService.ImpersonateAsync(userId, superId);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}

[ApiController]
[Route("api/agency")]
[Authorize(Roles = "Admin,Vendedor,Agencia,SuperAdmin")]
public class AgencyController(AgencyAdminService agencyService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "";

    private async Task<int?> PartnerIdOrBadRequest()
    {
        var pid = await agencyService.ResolvePartnerIdAsync(UserId, Role);
        if (!pid.HasValue && Role != "SuperAdmin")
            return null;
        if (Role == "SuperAdmin" && Request.Headers.TryGetValue("X-Partner-Id", out var h) && int.TryParse(h, out var fromHeader))
            return fromHeader;
        return pid;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetDashboardAsync(partnerId.Value);
        return r.Success ? Ok(r.Data) : NotFound(r);
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> Reservations()
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.GetReservationsAsync(partnerId.Value);
        return Ok(r.Data);
    }

    [HttpPut("reservations/{id:int}/status")]
    [Authorize(Roles = "Admin,Vendedor,Agencia,SuperAdmin")]
    public async Task<IActionResult> SetReservationStatus(int id, [FromQuery] string status = "Confirmed")
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.SetReservationStatusAsync(id, partnerId.Value, status);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("vendors")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.CreateVendorAsync(partnerId.Value, request, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("tours")]
    [Authorize(Roles = "Admin,Agencia,SuperAdmin")]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        var r = await agencyService.CreateTourAsync(request, UserId, Role);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("tours/{tourId:int}/capacity")]
    public async Task<IActionResult> UpdateCapacity(int tourId, [FromQuery] int available)
    {
        var partnerId = await PartnerIdOrBadRequest();
        if (!partnerId.HasValue) return Forbid();
        var r = await agencyService.UpdateTourCapacityAsync(tourId, partnerId.Value, available);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}
