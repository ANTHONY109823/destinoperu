using System.Security.Claims;
using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Services;
using DestinoPeruAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DestinoPeruAPI.API.Controllers;

[ApiController][Route("api/tours")]
public class ToursController(TourService tourService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] TourSearchQuery query)
    {
        try
        {
            return Ok(await tourService.SearchPagedAsync(query));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al listar tours.", detail = ex.Message });
        }
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var t = await tourService.GetBySlugAsync(slug);
        return t == null ? NotFound() : Ok(new ApiResponse<TourDto>(true, null, t));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await tourService.GetByIdAsync(id);
        return r.Success ? Ok(r) : NotFound(r);
    }

    [HttpPost][Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateTourRequest request)
    {
        var r = await tourService.CreateAsync(request, User.GetUserId());
        return r.Success ? Created("", r) : BadRequest(r);
    }

    [HttpDelete("{id:int}")][Authorize(Roles = "Agencia,Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await tourService.DeleteAsync(id, User.GetUserId(), User.GetRole());
        return r.Success ? Ok(r) : BadRequest(r);
    }
}

[ApiController][Route("api/destinations")]
public class DestinationsController(PopularDestinationService destinationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPublic() => Ok(await destinationService.GetPublicAsync());
}

[ApiController][Route("api/partners")]
public class PartnersController(PartnerService partnerService) : ControllerBase
{
    private int UserId => User.GetUserId();

    [HttpGet("pending")][Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetPending() => Ok(await partnerService.GetPendingAsync());

    [HttpPost][Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePartnerRequest request)
    {
        var r = await partnerService.CreateAsync(request, UserId);
        return r.Success ? Created("", r) : BadRequest(r);
    }

    [HttpPut("approve/{id:int}")][Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await partnerService.ApproveAsync(id);
        return r.Success ? Ok(r) : NotFound(r);
    }

    [HttpPost("{partnerId:int}/documents")][Authorize]
    public async Task<IActionResult> AddDocument(int partnerId, [FromBody] UploadDocumentRequest request)
    {
        var r = await partnerService.AddDocumentAsync(partnerId, UserId, request.DocumentType, request.FileUrl);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("documents/{id:int}/verify")][Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> VerifyDocument(int id, [FromQuery] bool approved)
    {
        var admin = User.GetClaim(ClaimTypes.Name) ?? "Admin";
        var r = await partnerService.VerifyDocumentAsync(id, admin, approved);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}

[ApiController][Route("api/agencies")]
public class AgenciesController(SuperAdminService superAdminService, PartnerService partnerService) : ControllerBase
{
    [HttpPost][Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePartnerRequest request)
    {
        var r = await partnerService.CreateAsync(request, User.GetUserId());
        return r.Success ? Created("", r) : BadRequest(r);
    }

    [HttpDelete("{id:int}")][Authorize(Roles = RoleNames.SuperAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await superAdminService.DeleteAgencyAsync(id);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPut("approve/{id:int}")][Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await partnerService.ApproveAsync(id);
        return r.Success ? Ok(r) : NotFound(r);
    }
}

[ApiController][Route("api/admin")][Authorize(Roles = "SuperAdmin")]
public class AdminController(PartnerService partnerService) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics() => Ok(await partnerService.GetAdminMetricsAsync());
}

[ApiController][Route("api/reservations")][Authorize]
public class ReservationsController(ReservationService reservationService) : ControllerBase
{
    private int UserId => User.GetUserId();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
    {
        var r = await reservationService.CreateAsync(request, UserId);
        return r.Success ? Created("", r) : BadRequest(r);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetByUser() => Ok(await reservationService.GetByUserAsync(UserId));

    [HttpGet("partner/{partnerId:int}")][Authorize(Roles = "Agencia,Admin,Vendedor,SuperAdmin")]
    public async Task<IActionResult> GetByPartner(int partnerId) =>
        Ok(await reservationService.GetByPartnerAsync(partnerId));

    [HttpPut("cancel/{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var r = await reservationService.CancelAsync(id, UserId);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}

[ApiController][Route("api/payments")][Authorize]
public class PaymentsController(PaymentService paymentService) : ControllerBase
{
    private int UserId => User.GetUserId();

    [HttpPost]
    public async Task<IActionResult> Process([FromBody] CreatePaymentRequest request)
    {
        var r = await paymentService.ProcessAsync(request, UserId);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("voucher")]
    public async Task<IActionResult> SubmitVoucher([FromBody] SubmitVoucherRequest request)
    {
        var r = await paymentService.SubmitVoucherAsync(request, UserId);
        return r.Success ? Ok(r) : BadRequest(r);
    }
}
